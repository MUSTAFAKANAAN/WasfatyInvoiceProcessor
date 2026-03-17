# Wasfaty Invoice Processor

A professional Windows desktop application for automating Wasfaty invoice processing and API submission. This application connects to your databases, fetches invoice data, and automatically submits it to the Wasfaty tracking API while maintaining complete audit logs.

## Features

✨ **Modern Material Design UI** - Beautiful, intuitive interface with Material Design components

📅 **Date-Based Processing** - Process invoices for single dates or date ranges

🔄 **Automatic Retry** - Smart retry logic for API authentication and submissions

📊 **Processing History** - Complete history of all processed dates with detailed statistics

🔍 **Detailed Logging** - Real-time activity logs with comprehensive audit trail

💾 **Database Tracking** - Stores all API requests, responses, and invoice details locally

⚡ **Batch Processing** - Process multiple days in sequence automatically

🛡️ **Error Handling** - Robust error handling with detailed error messages

## Prerequisites

- Windows 10/11
- .NET 8.0 Runtime or SDK
- SQL Server (not Express) running on localhost
- Access to remote SQL Server (10.10.8.182)
- Visual Studio 2022 (for building from source)

## Installation & Setup

### Step 1: Database Setup

1. **Create the local database:**
   - Open SQL Server Management Studio (SSMS)
   - Connect to your local SQL Server instance (localhost)
   - Open the file `DatabaseSetup.sql`
   - Execute the script to create the `WasfatyTracker` database and tables

2. **Verify database credentials:**
   - Local Server: localhost
   - Database: WasfatyTracker
   - User: sa
   - Password: 123

3. **Verify remote database access:**
   - Remote Server: 10.10.8.182
   - Database: rmsmainprod
   - User: sa
   - Password: P@ssw0rd

### Step 2: Build the Application

1. Open a terminal in the project directory:
   ```cmd
   cd d:\WasfatyTracker
   ```

2. Restore NuGet packages:
   ```cmd
   dotnet restore
   ```

3. Build the solution:
   ```cmd
   dotnet build --configuration Release
   ```

4. The executable will be located at:
   ```
   d:\WasfatyTracker\WasfatyInvoiceProcessor\bin\Release\net8.0-windows\WasfatyInvoiceProcessor.exe
   ```

### Step 3: Configuration

The application uses `appsettings.json` for configuration. The default settings are:

```json
{
  "Database": {
    "LocalConnectionString": "Server=localhost;Database=WasfatyTracker;User Id=sa;Password=123;TrustServerCertificate=True;",
    "RemoteConnectionString": "Server=10.10.8.182;Database=rmsmainprod;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;"
  },
  "Api": {
    "BaseUrl": "https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io",
    "LoginEndpoint": "/api/auth/login",
    "InvoiceEndpoint": "/api/wasfaty-tracking/invoices",
    "Email": "aa_oraiqat@unitedpharmacy.sa",
    "Password": "2179372228",
    "TimeoutSeconds": 120
  }
}
```

**If you need to change any settings:**
- Edit the `appsettings.json` file in the output directory
- Rebuild the application

## Usage

### First Time Setup

1. **Launch the application**
   - Double-click `WasfatyInvoiceProcessor.exe`

2. **Test connections**
   - Click the "TEST CONNECTIONS" button
   - Verify that both database connections and API authentication succeed
   - Check the activity log for any errors

### Processing Invoices

#### Process a Single Date

1. Select a date using the "Select Date" picker
2. Click "PROCESS DATE" to process that date
   - Or click "REPROCESS DATE" if the date was already processed

#### Process a Date Range

1. Set the "Start Date" (default: December 1, 2025)
2. Set the "End Date" (default: today)
3. Click "PROCESS RANGE" to process all dates in the range
   - Or click "REPROCESS RANGE" to reprocess already-processed dates

**Note:** When processing a range, the application will:
- Process each date sequentially
- Wait 1 second between dates to avoid API rate limits
- Show progress and status for each date
- Continue even if one date fails

### Understanding the Results

The application shows detailed results for each processing operation:

- **Total**: Number of invoices found for that date
- **Created**: Invoices successfully created in the API
- **Skipped**: Invoices already existing (duplicates)
- **Failed**: Invoices that failed to process

### Viewing History

The Processing History grid shows:
- **Date**: The processing date
- **Status**: Success, Failed, or Partial
- **Total/Success/Skipped/Failed**: Invoice counts
- **Started/Completed**: Processing timestamps
- **Message**: Summary message from the API

Click "REFRESH HISTORY" to reload the history grid.

### Generating Reports

Click "GENERATE REPORT" to create a summary report for all processed dates. The report appears in the activity log and includes:
- Total days processed
- Total invoices by status
- Daily breakdown

### Activity Log

The activity log at the bottom shows:
- Real-time processing status
- API authentication events
- Success/error messages
- Detailed results after each processing operation

Click "CLEAR" to clear the log.

## Database Schema

### Local Database: WasfatyTracker

#### ProcessingHistory
Tracks each date's processing status and results.

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| ProcessingDate | DATE | Date being processed |
| Status | NVARCHAR(50) | Success, Failed, Partial, Processing |
| TotalInvoices | INT | Total invoices found |
| SuccessCount | INT | Successfully created invoices |
| FailedCount | INT | Failed invoices |
| SkippedCount | INT | Skipped/duplicate invoices |
| StartedAt | DATETIME2 | Processing start time |
| CompletedAt | DATETIME2 | Processing completion time |
| ResponseMessage | NVARCHAR(500) | API response message |
| ErrorMessage | NVARCHAR(MAX) | Error details if failed |

#### APIRequest
Logs every API request made by the application.

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| ProcessingHistoryId | INT | Related processing record |
| RequestType | NVARCHAR(50) | Authentication or Invoice |
| RequestUrl | NVARCHAR(500) | Full API endpoint URL |
| RequestMethod | NVARCHAR(10) | POST, GET, etc. |
| RequestHeaders | NVARCHAR(MAX) | Request headers (JSON) |
| RequestBody | NVARCHAR(MAX) | Request payload |
| RequestedAt | DATETIME2 | Request timestamp |

#### APIResponse
Logs every API response received.

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| APIRequestId | INT | Related request record |
| StatusCode | INT | HTTP status code |
| IsSuccess | BIT | Success flag |
| ResponseHeaders | NVARCHAR(MAX) | Response headers |
| ResponseBody | NVARCHAR(MAX) | Response payload |
| ErrorMessage | NVARCHAR(MAX) | Error details if failed |
| ResponseReceivedAt | DATETIME2 | Response timestamp |
| DurationMs | INT | Response time in ms |

#### InvoiceDetails
Stores individual invoice processing details.

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| ProcessingHistoryId | INT | Related processing record |
| WasfatyInvoiceReference | NVARCHAR(50) | Unique invoice reference |
| Alias | NVARCHAR(20) | Pharmacy alias |
| InvoiceDateTime | DATETIME2 | Invoice date/time |
| CustomerName | NVARCHAR(200) | Customer name |
| CustomerPhone | NVARCHAR(20) | Customer phone |
| CustomerId | NVARCHAR(50) | Customer ID |
| ProcessingStatus | NVARCHAR(50) | Created, Skipped, Failed |
| ErrorMessage | NVARCHAR(MAX) | Error details if any |

## Troubleshooting

### Connection Issues

**Problem**: "Failed to connect to database"
- Verify SQL Server is running on localhost
- Confirm credentials: sa / 123
- Check that TCP/IP is enabled in SQL Server Configuration Manager
- Ensure SQL Server Authentication is enabled

**Problem**: "Remote database connection failed"
- Verify network connectivity to 10.10.8.182
- Check firewall rules allow SQL Server port (1433)
- Confirm remote server credentials

### API Issues

**Problem**: "Authentication failed"
- Verify API credentials in appsettings.json
- Check if the API endpoint is accessible
- Ensure internet connectivity

**Problem**: "Request timeout"
- Increase `TimeoutSeconds` in appsettings.json (default: 120)
- Check network stability

### Data Issues

**Problem**: "No invoices found"
- Verify the date has invoices meeting the criteria:
  - InvoiceTypeId = 8
  - Phone number length = 12
  - Net amount >= 100
- Check the remote database has data for that date

## API Response Format

The API returns responses in this format:

```json
{
    "success": true,
    "data": {
        "total": 5,
        "success": 1,
        "failed": 0,
        "skipped": 4,
        "errors": [
            {
                "index": 1,
                "reference": "260101200027010107",
                "error": "Duplicate - already exists"
            }
        ]
    },
    "message": "Processed 5 invoices: 1 created, 4 skipped, 0 failed"
}
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        MainWindow (UI)                       │
│  - Date Pickers, Buttons, DataGrid, Progress, Logs          │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              InvoiceProcessingService (Orchestration)        │
│  - Coordinates all operations                                │
│  - Handles business logic                                    │
│  - Reports status and progress                               │
└──────────┬──────────────┬──────────────┬────────────────────┘
           │              │              │
           ▼              ▼              ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  LocalDB     │  │  RemoteDB    │  │  WasfatyApi  │
│  Service     │  │  Service     │  │  Service     │
│              │  │              │  │              │
│ - Logging    │  │ - Fetch      │  │ - Auth       │
│ - History    │  │   Invoices   │  │ - Submit     │
│ - Tracking   │  │ - Query SQL  │  │   Invoices   │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                 │
       ▼                 ▼                 ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  WasfatyTracker│  │  rmsmainprod │  │  API         │
│  (localhost)  │  │  (10.10.8.182)│  │  Endpoint    │
└──────────────┘  └──────────────┘  └──────────────┘
```

## Technologies Used

- **.NET 8.0** - Application framework
- **WPF** - Windows Presentation Foundation for UI
- **Material Design** - Modern UI components and theming
- **Dapper** - Lightweight ORM for database operations
- **Microsoft.Data.SqlClient** - SQL Server connectivity
- **Newtonsoft.Json** - JSON serialization
- **HttpClient** - REST API communication

## License

Internal tool for United Pharmacy use only.

## Support

For issues or questions, contact the development team.

---

**Version:** 1.0.0  
**Last Updated:** January 3, 2026  
**Author:** Development Team
