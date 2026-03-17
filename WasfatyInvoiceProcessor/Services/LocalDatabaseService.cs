using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using WasfatyInvoiceProcessor.Models;

namespace WasfatyInvoiceProcessor.Services;

public class LocalDatabaseService
{
    private readonly string _connectionString;

    public LocalDatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // =============================================
    // ProcessingHistory Methods
    // =============================================

    public async Task<int> CreateProcessingHistoryAsync(DateTime processingDate)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO ProcessingHistory (ProcessingDate, Status, StartedAt, CreatedAt, UpdatedAt)
            VALUES (@ProcessingDate, 'Processing', GETDATE(), GETDATE(), GETDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        return await connection.ExecuteScalarAsync<int>(sql, new { ProcessingDate = processingDate });
    }

    public async Task UpdateProcessingHistoryAsync(int id, string status, int totalInvoices, 
        int successCount, int failedCount, int skippedCount, string? responseMessage = null, string? errorMessage = null)
    {
        using var connection = CreateConnection();
        const string sql = @"
            UPDATE ProcessingHistory 
            SET Status = @Status,
                TotalInvoices = @TotalInvoices,
                SuccessCount = @SuccessCount,
                FailedCount = @FailedCount,
                SkippedCount = @SkippedCount,
                CompletedAt = GETDATE(),
                ResponseMessage = @ResponseMessage,
                ErrorMessage = @ErrorMessage,
                UpdatedAt = GETDATE()
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Status = status,
            TotalInvoices = totalInvoices,
            SuccessCount = successCount,
            FailedCount = failedCount,
            SkippedCount = skippedCount,
            ResponseMessage = responseMessage,
            ErrorMessage = errorMessage
        });
    }

    public async Task<List<ProcessingHistory>> GetProcessingHistoryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        using var connection = CreateConnection();
        const string sql = @"
            SELECT * FROM ProcessingHistory
            WHERE (@StartDate IS NULL OR ProcessingDate >= @StartDate)
              AND (@EndDate IS NULL OR ProcessingDate <= @EndDate)
            ORDER BY ProcessingDate DESC";

        var result = await connection.QueryAsync<ProcessingHistory>(sql, new { StartDate = startDate, EndDate = endDate });
        return result.ToList();
    }

    public async Task<ProcessingHistory?> GetProcessingHistoryByDateAsync(DateTime processingDate)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM ProcessingHistory WHERE ProcessingDate = @ProcessingDate";
        return await connection.QueryFirstOrDefaultAsync<ProcessingHistory>(sql, new { ProcessingDate = processingDate });
    }

    public async Task<bool> IsDateProcessedAsync(DateTime processingDate)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT COUNT(1) FROM ProcessingHistory WHERE ProcessingDate = @ProcessingDate AND Status = 'Success'";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { ProcessingDate = processingDate });
        return count > 0;
    }

    // =============================================
    // APIRequest Methods
    // =============================================

    public async Task<int> LogAPIRequestAsync(int? processingHistoryId, string requestType, 
        string requestUrl, string requestMethod, string? requestHeaders = null, string? requestBody = null)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO APIRequest (ProcessingHistoryId, RequestType, RequestUrl, RequestMethod, 
                RequestHeaders, RequestBody, RequestedAt)
            VALUES (@ProcessingHistoryId, @RequestType, @RequestUrl, @RequestMethod, 
                @RequestHeaders, @RequestBody, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        var parameters = new DynamicParameters();
        parameters.Add("@ProcessingHistoryId", processingHistoryId);
        parameters.Add("@RequestType", requestType);
        parameters.Add("@RequestUrl", requestUrl);
        parameters.Add("@RequestMethod", requestMethod);
        parameters.Add("@RequestHeaders", requestHeaders, System.Data.DbType.String, size: -1);
        parameters.Add("@RequestBody", requestBody, System.Data.DbType.String, size: -1);

        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    // =============================================
    // APIResponse Methods
    // =============================================

    public async Task LogAPIResponseAsync(int apiRequestId, int statusCode, bool isSuccess, 
        string? responseHeaders = null, string? responseBody = null, string? errorMessage = null, int? durationMs = null)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO APIResponse (APIRequestId, StatusCode, IsSuccess, ResponseHeaders, 
                ResponseBody, ErrorMessage, ResponseReceivedAt, DurationMs)
            VALUES (@APIRequestId, @StatusCode, @IsSuccess, @ResponseHeaders, 
                @ResponseBody, @ErrorMessage, GETDATE(), @DurationMs)";

        var parameters = new DynamicParameters();
        parameters.Add("@APIRequestId", apiRequestId);
        parameters.Add("@StatusCode", statusCode);
        parameters.Add("@IsSuccess", isSuccess);
        parameters.Add("@ResponseHeaders", responseHeaders, System.Data.DbType.String, size: -1);
        parameters.Add("@ResponseBody", responseBody, System.Data.DbType.String, size: -1);
        parameters.Add("@ErrorMessage", errorMessage);
        parameters.Add("@DurationMs", durationMs);

        await connection.ExecuteAsync(sql, parameters);
    }

    // =============================================
    // InvoiceDetails Methods
    // =============================================

    public async Task SaveInvoiceDetailsAsync(int processingHistoryId, List<InvoiceData> invoices, 
        List<InvoiceError> errors)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string sql = @"
                INSERT INTO InvoiceDetails (ProcessingHistoryId, WasfatyInvoiceReference, Alias, 
                    InvoiceDateTime, CustomerName, CustomerPhone, CustomerId, ProcessingStatus, 
                    ErrorMessage, CreatedAt)
                VALUES (@ProcessingHistoryId, @WasfatyInvoiceReference, @Alias, 
                    @InvoiceDateTime, @CustomerName, @CustomerPhone, @CustomerId, @ProcessingStatus, 
                    @ErrorMessage, GETDATE())";

            // Save all invoices with their status
            foreach (var invoice in invoices)
            {
                var error = errors.FirstOrDefault(e => e.Reference == invoice.WasfatyInvoiceReference);
                var status = error != null ? (error.Error.Contains("Duplicate") ? "Skipped" : "Failed") : "Created";

                await connection.ExecuteAsync(sql, new
                {
                    ProcessingHistoryId = processingHistoryId,
                    WasfatyInvoiceReference = invoice.WasfatyInvoiceReference,
                    Alias = invoice.Alias,
                    InvoiceDateTime = DateTime.Parse(invoice.InvoiceDateTime),
                    CustomerName = invoice.CustomerName,
                    CustomerPhone = invoice.CustomerPhone,
                    CustomerId = invoice.CustomerId,
                    ProcessingStatus = status,
                    ErrorMessage = error?.Error
                }, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // =============================================
    // Statistics Methods
    // =============================================

    public async Task<Dictionary<DateTime, string>> GetProcessedDatesWithStatusAsync(DateTime startDate, DateTime endDate)
    {
        using var connection = CreateConnection();
        const string sql = @"
            SELECT ProcessingDate, Status 
            FROM ProcessingHistory 
            WHERE ProcessingDate BETWEEN @StartDate AND @EndDate
            ORDER BY ProcessingDate";

        var result = await connection.QueryAsync<(DateTime ProcessingDate, string Status)>(sql, 
            new { StartDate = startDate, EndDate = endDate });

        return result.ToDictionary(x => x.ProcessingDate.Date, x => x.Status);
    }
}
