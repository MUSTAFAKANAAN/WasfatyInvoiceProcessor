using AutoUpdaterDotNET;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using WasfatyInvoiceProcessor.Models;
using WasfatyInvoiceProcessor.Services;

namespace WasfatyInvoiceProcessor;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly LocalDatabaseService _localDb;
    private readonly RemoteDatabaseService _remoteDb;
    private readonly WasfatyApiService _apiService;
    private readonly InvoiceProcessingService _processingService;

    private bool _isProcessing;
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged(nameof(IsProcessing));
            UpdateControlsState();
        }
    }

    public string AppVersion { get; } =
        $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // Load configuration from embedded resource
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("WasfatyInvoiceProcessor.appsettings.json")
            ?? throw new Exception("Embedded appsettings.json not found.");

        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        var settings = config.Get<AppSettings>() ?? throw new Exception("Failed to load settings");

        // Initialize services
        _localDb = new LocalDatabaseService(settings.Database.LocalConnectionString);
        _remoteDb = new RemoteDatabaseService(settings.Database.RemoteConnectionString);
        _apiService = new WasfatyApiService(
            _localDb,
            settings.Api.BaseUrl,
            settings.Api.LoginEndpoint,
            settings.Api.InvoiceEndpoint,
            settings.Api.Email,
            settings.Api.Password,
            settings.Api.TimeoutSeconds
        );

        _processingService = new InvoiceProcessingService(_localDb, _remoteDb, _apiService);
        _processingService.StatusChanged += OnStatusChanged;
        _processingService.ProgressChanged += OnProgressChanged;

        // Set default dates
        SingleDatePicker.SelectedDate = DateTime.Today;
        StartDatePicker.SelectedDate = new DateTime(2025, 12, 1);
        EndDatePicker.SelectedDate = DateTime.Today;

        // Load initial data
        LoadHistoryAsync();
        LogMessage("Application started. Ready to process invoices.");
        
        // Check connections on startup
        CheckConnectionsAsync();
    }

    private async void CheckConnectionsAsync()
    {
        try
        {
            UpdateConnectionStatus("Checking...", "Orange");
            
            var success = await _processingService.TestConnectionsAsync();
            
            if (success)
            {
                UpdateConnectionStatus("Connected", "Green");
            }
            else
            {
                UpdateConnectionStatus("Connection Failed", "Red");
            }
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus("Error", "Red");
            LogMessage($"Connection check error: {ex.Message}");
        }
    }

    private void UpdateConnectionStatus(string status, string color)
    {
        ConnectionStatusText.Text = status;
        
        // Update the connection status indicator (Ellipse)
        var brush = color switch
        {
            "Green" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(67, 160, 71)),  // Modern green
            "Red" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 57, 53)),    // Modern red
            "Orange" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 140, 0)),  // Modern orange
            _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
        };
        
        ConnectionStatusIcon.Fill = brush;
    }

    private void UpdateControlsState()
    {
        ProcessSingleButton.IsEnabled = !IsProcessing;
        ReprocessSingleButton.IsEnabled = !IsProcessing;
        ProcessRangeButton.IsEnabled = !IsProcessing;
        ReprocessRangeButton.IsEnabled = !IsProcessing;
        TestConnectionButton.IsEnabled = !IsProcessing;
        RefreshHistoryButton.IsEnabled = !IsProcessing;
        GenerateReportButton.IsEnabled = !IsProcessing;
        SingleDatePicker.IsEnabled = !IsProcessing;
        StartDatePicker.IsEnabled = !IsProcessing;
        EndDatePicker.IsEnabled = !IsProcessing;

        // Show/hide the modern progress overlay
        ProgressOverlay.Visibility = IsProcessing ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        AutoUpdater.ReportErrors = true;
        AutoUpdater.Start("https://raw.githubusercontent.com/MUSTAFAKANAAN/WasfatyInvoiceProcessor/master/update.xml");
    }

    private void OnStatusChanged(object? sender, string status)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = status;
            LogMessage(status);
        });
    }

    private void OnProgressChanged(object? sender, int progress)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = progress;
            ProgressText.Text = $"{progress}%";
        });
    }

    private void LogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogTextBox.AppendText($"[{timestamp}] {message}\n");
        LogTextBox.ScrollToEnd();
    }

    private async void LoadHistoryAsync()
    {
        try
        {
            var startDate = new DateTime(2025, 12, 1);
            var endDate = DateTime.Today;

            var history = await _localDb.GetProcessingHistoryAsync(startDate, endDate);
            HistoryDataGrid.ItemsSource = history;
            HistoryCountText.Text = $"({history.Count} records)";
        }
        catch (Exception ex)
        {
            LogMessage($"Error loading history: {ex.Message}");
            MessageBox.Show($"Error loading history: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ProcessSingleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!SingleDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select a date to process.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ProcessDateAsync(SingleDatePicker.SelectedDate.Value, forceReprocess: false);
    }

    private async void ReprocessSingleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!SingleDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select a date to reprocess.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to reprocess {SingleDatePicker.SelectedDate.Value:yyyy-MM-dd}?\n\n" +
            "This will create a new processing record.",
            "Confirm Reprocess",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await ProcessDateAsync(SingleDatePicker.SelectedDate.Value, forceReprocess: true);
        }
    }

    private async void ProcessRangeButton_Click(object sender, RoutedEventArgs e)
    {
        await ProcessDateRangeAsync(forceReprocess: false);
    }

    private async void ReprocessRangeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select both start and end dates.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dayCount = (EndDatePicker.SelectedDate.Value - StartDatePicker.SelectedDate.Value).Days + 1;

        var result = MessageBox.Show(
            $"Are you sure you want to reprocess {dayCount} days?\n\n" +
            $"Range: {StartDatePicker.SelectedDate.Value:yyyy-MM-dd} to {EndDatePicker.SelectedDate.Value:yyyy-MM-dd}\n\n" +
            "This may take several minutes.",
            "Confirm Reprocess Range",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await ProcessDateRangeAsync(forceReprocess: true);
        }
    }

    private async Task ProcessDateAsync(DateTime date, bool forceReprocess)
    {
        IsProcessing = true;
        try
        {
            LogMessage($"{'='.ToString().PadRight(60, '=')}");
            LogMessage($"Starting processing for {date:yyyy-MM-dd}");
            
            var result = await _processingService.ProcessDateAsync(date, forceReprocess);

            LogMessage($"Result: {(result.Success ? "SUCCESS" : "FAILED")}");
            LogMessage($"  Total Invoices: {result.TotalInvoices}");
            LogMessage($"  Created: {result.SuccessCount}");
            LogMessage($"  Skipped: {result.SkippedCount}");
            LogMessage($"  Failed: {result.FailedCount}");
            LogMessage($"  Message: {result.Message}");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                LogMessage($"  Error: {result.ErrorMessage}");
            }

            if (result.Errors.Any())
            {
                LogMessage($"  Errors Details:");
                foreach (var error in result.Errors.Take(10))
                {
                    LogMessage($"    - {error.Reference}: {error.Error}");
                }
                if (result.Errors.Count > 10)
                {
                    LogMessage($"    ... and {result.Errors.Count - 10} more errors");
                }
            }

            LoadHistoryAsync();

            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage ?? "Processing failed", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (result.SuccessCount > 0)
            {
                MessageBox.Show(result.Message, "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Exception: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            ProgressBar.Value = 0;
            StatusText.Text = "Ready";
        }
    }

    private async Task ProcessDateRangeAsync(bool forceReprocess)
    {
        if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select both start and end dates.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (StartDatePicker.SelectedDate.Value > EndDatePicker.SelectedDate.Value)
        {
            MessageBox.Show("Start date must be before or equal to end date.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var startDate = StartDatePicker.SelectedDate.Value.Date;
        var endDate   = EndDatePicker.SelectedDate.Value.Date;
        var totalDays = (int)(endDate - startDate).TotalDays + 1;

        IsProcessing = true;

        // Replace progress handler with a range-aware one that shows overall progress
        _processingService.ProgressChanged -= OnProgressChanged;
        int currentDayIndex = 0;
        EventHandler<int> rangeProgressHandler = (_, dayProgress) =>
        {
            Dispatcher.Invoke(() =>
            {
                var overall = (int)(((currentDayIndex - 1) * 100.0 + dayProgress) / totalDays);
                ProgressBar.Value = overall;
                ProgressText.Text = $"{overall}%  (Day {currentDayIndex}/{totalDays})";
            });
        };
        _processingService.ProgressChanged += rangeProgressHandler;

        try
        {
            LogMessage($"{'='.ToString().PadRight(60, '=')}");
            LogMessage($"Starting range processing: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd} ({totalDays} day(s))");

            int totalInvoices = 0, totalSuccess = 0, totalSkipped = 0, totalFailed = 0, successDays = 0;
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                currentDayIndex++;
                LogMessage($"");
                LogMessage($"--- Day {currentDayIndex}/{totalDays}: {currentDate:yyyy-MM-dd} ---");

                var result = await _processingService.ProcessDateAsync(currentDate, forceReprocess);

                LogMessage($"  Result:   {(result.Success ? "SUCCESS" : "FAILED")}");
                LogMessage($"  Total:    {result.TotalInvoices}");
                LogMessage($"  Created:  {result.SuccessCount}");
                LogMessage($"  Skipped:  {result.SkippedCount}");
                LogMessage($"  Failed:   {result.FailedCount}");
                LogMessage($"  Message:  {result.Message}");

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    LogMessage($"  Error:    {result.ErrorMessage}");

                if (result.Errors.Any())
                {
                    LogMessage($"  Error Details:");
                    foreach (var error in result.Errors.Take(10))
                        LogMessage($"    - {error.Reference}: {error.Error}");
                    if (result.Errors.Count > 10)
                        LogMessage($"    ... and {result.Errors.Count - 10} more errors");
                }

                if (result.Success) successDays++;
                totalInvoices += result.TotalInvoices;
                totalSuccess  += result.SuccessCount;
                totalSkipped  += result.SkippedCount;
                totalFailed   += result.FailedCount;

                currentDate = currentDate.AddDays(1);
            }

            LogMessage($"");
            LogMessage($"{'='.ToString().PadRight(60, '=')}");
            LogMessage($"Range processing completed!");
            LogMessage($"  Days processed:   {totalDays}");
            LogMessage($"  Successful days:  {successDays}");
            LogMessage($"  Total invoices:   {totalInvoices}");
            LogMessage($"  Created:          {totalSuccess}");
            LogMessage($"  Skipped:          {totalSkipped}");
            LogMessage($"  Failed:           {totalFailed}");

            LoadHistoryAsync();

            MessageBox.Show(
                $"Range processing completed!\n\n" +
                $"Days processed: {totalDays}\n" +
                $"Successful days: {successDays}\n\n" +
                $"Total invoices: {totalInvoices}\n" +
                $"Created:  {totalSuccess}\n" +
                $"Skipped:  {totalSkipped}\n" +
                $"Failed:   {totalFailed}",
                "Range Processing Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogMessage($"Exception: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Restore original progress handler
            _processingService.ProgressChanged -= rangeProgressHandler;
            _processingService.ProgressChanged += OnProgressChanged;

            IsProcessing = false;
            ProgressBar.Value = 0;
            StatusText.Text = "Ready";
        }
    }

    private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        IsProcessing = true;
        try
        {
            LogMessage("Testing connections...");
            UpdateConnectionStatus("Checking...", "Orange");
            
            var success = await _processingService.TestConnectionsAsync();

            if (success)
            {
                UpdateConnectionStatus("Connected", "Green");
                MessageBox.Show("All connections successful!", "Connection Test", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                UpdateConnectionStatus("Connection Failed", "Red");
                MessageBox.Show("Connection test failed. Check the log for details.", "Connection Test", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus("Error", "Red");
            LogMessage($"Exception: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void RefreshHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        LoadHistoryAsync();
        LogMessage("History refreshed.");
    }

    private async void GenerateReportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var startDate = new DateTime(2025, 12, 1);
            var endDate = DateTime.Today;

            var report = await _processingService.GenerateSummaryReportAsync(startDate, endDate);

            LogMessage("Summary report generated:");
            LogMessage(report);

            MessageBox.Show("Report generated and added to the log.", "Report Generated", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogMessage($"Error generating report: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
    }
}
