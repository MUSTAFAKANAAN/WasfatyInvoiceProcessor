using System.Text;
using System.IO;
using WasfatyInvoiceProcessor.Models;

namespace WasfatyInvoiceProcessor.Services;

public class ProcessingResult
{
    public bool Success { get; set; }
    public DateTime ProcessingDate { get; set; }
    public int TotalInvoices { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public List<InvoiceError> Errors { get; set; } = new();
}

public class InvoiceProcessingService
{
    private readonly LocalDatabaseService _localDb;
    private readonly RemoteDatabaseService _remoteDb;
    private readonly WasfatyApiService _apiService;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<int>? ProgressChanged;

    public InvoiceProcessingService(
        LocalDatabaseService localDb,
        RemoteDatabaseService remoteDb,
        WasfatyApiService apiService)
    {
        _localDb = localDb;
        _remoteDb = remoteDb;
        _apiService = apiService;
    }

    private void ReportStatus(string status)
    {
        StatusChanged?.Invoke(this, status);
    }

    private void ReportProgress(int percentage)
    {
        ProgressChanged?.Invoke(this, percentage);
    }

    public async Task<ProcessingResult> ProcessDateAsync(DateTime targetDate, bool forceReprocess = false)
    {
        var result = new ProcessingResult
        {
            ProcessingDate = targetDate
        };

        int? processingHistoryId = null;

        try
        {
            ReportStatus($"Starting processing for {targetDate:yyyy-MM-dd}...");
            ReportProgress(0);

            // Check if already processed
            if (!forceReprocess)
            {
                var existing = await _localDb.GetProcessingHistoryByDateAsync(targetDate);
                if (existing != null && existing.Status == "Success")
                {
                    result.Success = false;
                    result.ErrorMessage = $"Date {targetDate:yyyy-MM-dd} has already been processed successfully. Use 'Reprocess' to process again.";
                    return result;
                }
            }

            // Create processing history record
            ReportStatus("Creating processing record...");
            processingHistoryId = await _localDb.CreateProcessingHistoryAsync(targetDate);
            ReportProgress(10);

            // Authenticate with API
            ReportStatus("Authenticating with Wasfaty API...");
            var (authSuccess, token, authError) = await _apiService.AuthenticateAsync(processingHistoryId);

            if (!authSuccess)
            {
                result.Success = false;
                result.ErrorMessage = $"Authentication failed: {authError}";
                await _localDb.UpdateProcessingHistoryAsync(
                    processingHistoryId.Value,
                    "Failed",
                    0, 0, 0, 0,
                    null,
                    result.ErrorMessage
                );
                return result;
            }
            ReportProgress(20);

            // Fetch invoices from remote database
            ReportStatus($"Fetching invoices from remote database for {targetDate:yyyy-MM-dd}...");
            var invoices = await _remoteDb.GetInvoicesForDateAsync(targetDate);

            if (invoices == null || invoices.Count == 0)
            {
                result.Success = true;
                result.Message = $"No invoices found for {targetDate:yyyy-MM-dd}";
                result.TotalInvoices = 0;

                await _localDb.UpdateProcessingHistoryAsync(
                    processingHistoryId.Value,
                    "Success",
                    0, 0, 0, 0,
                    result.Message,
                    null
                );

                ReportStatus("No invoices found.");
                ReportProgress(100);
                return result;
            }

            result.TotalInvoices = invoices.Count;
            ReportStatus($"Found {invoices.Count} invoices. Preparing request...");
            
            // Serialize to JSON and save to file for easy testing
            var requestJson = Newtonsoft.Json.JsonConvert.SerializeObject(invoices, Newtonsoft.Json.Formatting.Indented);
            var requestFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_request.json");
            await File.WriteAllTextAsync(requestFilePath, requestJson);
            ReportStatus($"Request body saved to: {requestFilePath}");
            ReportStatus($"Request size: {requestJson.Length} characters ({invoices.Count} invoices)");
            ReportStatus("Submitting to API...");
            ReportProgress(50);

            // Submit invoices to API
            var (submitSuccess, apiResponse, submitError) = await _apiService.SubmitInvoicesAsync(
                invoices, 
                processingHistoryId.Value
            );

            if (!submitSuccess || apiResponse == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to submit invoices: {submitError}";
                
                await _localDb.UpdateProcessingHistoryAsync(
                    processingHistoryId.Value,
                    "Failed",
                    invoices.Count, 0, 0, 0,
                    null,
                    result.ErrorMessage
                );

                ReportStatus($"Failed to submit invoices: {submitError}");
                ReportProgress(100);
                return result;
            }

            ReportProgress(80);

            // Process API response
            result.Success = apiResponse.Success;
            result.SuccessCount = apiResponse.Data?.Success ?? 0;
            result.FailedCount = apiResponse.Data?.Failed ?? 0;
            result.SkippedCount = apiResponse.Data?.Skipped ?? 0;
            result.Message = apiResponse.Message;
            result.Errors = apiResponse.Data?.Errors ?? new List<InvoiceError>();

            // Save invoice details
            ReportStatus("Saving invoice details...");
            await _localDb.SaveInvoiceDetailsAsync(processingHistoryId.Value, invoices, result.Errors);
            ReportProgress(90);

            // Update processing history
            var status = result.SuccessCount > 0 ? "Success" : 
                        (result.FailedCount > 0 ? "Failed" : "Partial");

            await _localDb.UpdateProcessingHistoryAsync(
                processingHistoryId.Value,
                status,
                result.TotalInvoices,
                result.SuccessCount,
                result.FailedCount,
                result.SkippedCount,
                result.Message,
                null
            );

            ReportStatus($"Processing completed: {result.Message}");
            ReportProgress(100);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Unexpected error: {ex.Message}";

            if (processingHistoryId.HasValue)
            {
                await _localDb.UpdateProcessingHistoryAsync(
                    processingHistoryId.Value,
                    "Failed",
                    result.TotalInvoices, 0, 0, 0,
                    null,
                    result.ErrorMessage
                );
            }

            ReportStatus($"Error: {ex.Message}");
            ReportProgress(100);

            return result;
        }
    }

    public async Task<List<ProcessingResult>> ProcessDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        bool forceReprocess = false)
    {
        var results = new List<ProcessingResult>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var result = await ProcessDateAsync(currentDate, forceReprocess);
            results.Add(result);

            // Small delay between processing to avoid overwhelming the API
            if (currentDate < endDate.Date)
            {
                await Task.Delay(1000);
            }

            currentDate = currentDate.AddDays(1);
        }

        return results;
    }

    public async Task<bool> TestConnectionsAsync()
    {
        try
        {
            ReportStatus("Testing remote database connection...");
            var remoteTest = await _remoteDb.TestConnectionAsync();

            if (!remoteTest)
            {
                ReportStatus("Remote database connection failed!");
                return false;
            }

            ReportStatus("Testing API authentication...");
            var (authSuccess, _, authError) = await _apiService.AuthenticateAsync();

            if (!authSuccess)
            {
                ReportStatus($"API authentication failed: {authError}");
                return false;
            }

            ReportStatus("All connections successful!");
            return true;
        }
        catch (Exception ex)
        {
            ReportStatus($"Connection test failed: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GenerateSummaryReportAsync(DateTime startDate, DateTime endDate)
    {
        var history = await _localDb.GetProcessingHistoryAsync(startDate, endDate);

        var report = new StringBuilder();
        report.AppendLine("=".PadRight(60, '='));
        report.AppendLine($"PROCESSING SUMMARY REPORT");
        report.AppendLine($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        report.AppendLine("=".PadRight(60, '='));
        report.AppendLine();

        var totalDays = history.Count;
        var successDays = history.Count(h => h.Status == "Success");
        var totalInvoices = history.Sum(h => h.TotalInvoices);
        var totalSuccess = history.Sum(h => h.SuccessCount);
        var totalFailed = history.Sum(h => h.FailedCount);
        var totalSkipped = history.Sum(h => h.SkippedCount);

        report.AppendLine($"Total Days Processed: {totalDays}");
        report.AppendLine($"Successful Days: {successDays}");
        report.AppendLine($"Total Invoices: {totalInvoices}");
        report.AppendLine($"  - Created: {totalSuccess}");
        report.AppendLine($"  - Skipped: {totalSkipped}");
        report.AppendLine($"  - Failed: {totalFailed}");
        report.AppendLine();
        report.AppendLine("Daily Breakdown:");
        report.AppendLine("-".PadRight(60, '-'));

        foreach (var item in history.OrderBy(h => h.ProcessingDate))
        {
            report.AppendLine($"{item.ProcessingDate:yyyy-MM-dd} | {item.Status,-10} | " +
                            $"Total: {item.TotalInvoices,3} | " +
                            $"Success: {item.SuccessCount,3} | " +
                            $"Skipped: {item.SkippedCount,3} | " +
                            $"Failed: {item.FailedCount,3}");
        }

        report.AppendLine("=".PadRight(60, '='));

        return report.ToString();
    }
}
