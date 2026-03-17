# Wasfaty Invoice Processor

A professional Windows desktop application for automating Wasfaty invoice processing and API submission. Built with WPF and .NET 8.0, featuring a modern Material Design UI.

## 🌟 Features

- ✅ **Modern UI** - Beautiful, intuitive interface with Material Design components
- 📅 **Date-Based Processing** - Process invoices for single dates or date ranges
- 🔄 **Automatic Retry** - Smart retry logic for API authentication and submissions
- 📊 **Processing History** - Complete history with detailed statistics
- 🔍 **Detailed Logging** - Real-time activity logs with comprehensive audit trail
- 💾 **Database Tracking** - Stores all API requests, responses, and invoice details
- ⚡ **Batch Processing** - Process multiple days in sequence automatically
- 🛡️ **Error Handling** - Robust error handling with detailed error messages
- 🧹 **Character Sanitization** - Automatic cleaning of special characters
- 📦 **Large Response Handling** - Supports processing 1000+ invoices per batch

## 🖼️ Screenshots

Modern gradient header with professional UI design, organized card-based layout, and real-time processing feedback.

## 📋 Prerequisites

- **Windows 10/11**
- **.NET 8.0 Desktop Runtime** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **SQL Server** (NOT Express Edition) for local tracking database
- **Network Access** to:
  - Remote SQL Server (for invoice data)
  - Wasfaty API endpoint

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/YOUR_USERNAME/WASFATYTRACKER.git
cd WASFATYTRACKER
```

### 2. Setup Configuration

Copy the template and fill in your credentials:

```bash
copy WasfatyInvoiceProcessor\appsettings.template.json WasfatyInvoiceProcessor\appsettings.json
```

Edit `appsettings.json` with your database credentials and API keys.

### 3. Setup Database

Run the database setup script in SQL Server Management Studio:

```sql
-- Execute DatabaseSetup.sql on your local SQL Server
```

### 4. Build & Run

```bash
dotnet restore
dotnet build --configuration Release
dotnet run --project WasfatyInvoiceProcessor
```

Or open `WasfatyInvoiceProcessor.sln` in Visual Studio 2022.

## 📦 Deployment

To create a deployment package:

```bash
dotnet publish WasfatyInvoiceProcessor/WasfatyInvoiceProcessor.csproj --configuration Release --output Publish
```

See [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) for detailed deployment instructions.

## 🗂️ Project Structure

```
WasfatyTracker/
├── WasfatyInvoiceProcessor/      # Main application
│   ├── Models/                   # Data models
│   ├── Services/                 # Business logic services
│   ├── MainWindow.xaml           # Modern UI
│   └── appsettings.template.json # Configuration template
├── DatabaseSetup.sql              # Database schema
├── README.md                      # This file
└── Documentation/
    ├── DEPLOYMENT_GUIDE.md        # Deployment instructions
    ├── CHARACTER_SANITIZATION.md  # Text cleaning rules
    ├── API_ERROR_FIX.md          # API troubleshooting
    └── UI_REDESIGN_SUMMARY.md    # UI documentation
```

## 🔧 Configuration

The `appsettings.json` file contains:

```json
{
  "Database": {
    "LocalConnectionString": "Server=YOUR_SERVER;Database=WasfatyTracker;...",
    "RemoteConnectionString": "Server=YOUR_REMOTE;Database=rmsmainprod;..."
  },
  "Api": {
    "BaseUrl": "https://your-api-url",
    "Email": "your-email",
    "Password": "your-password",
    "TimeoutSeconds": 300
  }
}
```

⚠️ **Security**: Never commit `appsettings.json` with real credentials!

## 📊 Database Schema

### Local Database: WasfatyTracker

- **ProcessingHistory** - Tracks each date's processing status
- **APIRequest** - Logs all API requests
- **APIResponse** - Logs all API responses
- **InvoiceDetails** - Stores individual invoice processing details

See [DatabaseSetup.sql](DatabaseSetup.sql) for complete schema.

## 🎨 Modern UI Features

- **Gradient Header** - Professional blue-to-teal gradient
- **Card-Based Layout** - Organized sections with elevation
- **Color-Coded Results** - Green (success), Orange (skipped), Red (failed)
- **Responsive Design** - Adapts to different screen sizes
- **Real-Time Feedback** - Progress indicators and activity logs

## 🔍 Key Features Detail

### Single Date Processing
Process invoices for a specific date with validation and error handling.

### Date Range Processing
Batch process multiple days automatically with progress tracking.

### Processing History
View complete history of all processed dates with:
- Total invoices
- Success/Failed/Skipped counts
- Processing timestamps
- Response messages

### Activity Log
Real-time logs showing:
- Connection tests
- API authentication
- Processing progress
- Detailed error messages

### Character Sanitization
Automatically removes special characters from:
- Product descriptions
- Customer names

Excluded characters: `( ) + = & # @ ! : ; % *` and more.

## 🛠️ Technologies Used

- **.NET 8.0** - Modern application framework
- **WPF** - Windows Presentation Foundation for rich UI
- **Material Design** - Modern UI components and theming
- **Dapper** - Lightweight ORM for database operations
- **Microsoft.Data.SqlClient** - SQL Server connectivity
- **Newtonsoft.Json** - JSON serialization
- **HttpClient** - REST API communication

## 📖 Documentation

- [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) - Complete deployment instructions
- [CHARACTER_SANITIZATION.md](CHARACTER_SANITIZATION.md) - Text cleaning documentation
- [API_ERROR_FIX.md](API_ERROR_FIX.md) - API troubleshooting guide
- [LARGE_RESPONSE_FIX.md](LARGE_RESPONSE_FIX.md) - Large data handling
- [UI_REDESIGN_SUMMARY.md](UI_REDESIGN_SUMMARY.md) - UI design documentation

## 🐛 Troubleshooting

### Connection Failed
- Verify SQL Server is running
- Check connection strings in `appsettings.json`
- Test network connectivity

### API Authentication Failed
- Verify API credentials
- Check if account is locked (wait for unlock)
- Ensure API endpoint is accessible

### No Invoices Found
- Verify date has invoices in remote database
- Check invoice criteria (Type 8, Phone length 12, Amount >= 100)

See [API_ERROR_FIX.md](API_ERROR_FIX.md) for detailed troubleshooting.

## 🔐 Security Notes

- **Never commit** `appsettings.json` with real credentials
- Use `.gitignore` to exclude sensitive files
- Store credentials securely
- Use environment variables or secret management in production

## 📝 License

Internal tool for authorized use only.

## 👥 Contributing

This is an internal project. Contact the development team for access.

## 📞 Support

For issues or questions:
1. Check the documentation in the repository
2. Review the Activity Log for error details
3. Contact your system administrator

## 🚀 Future Enhancements

- [ ] Multi-user support
- [ ] Email notifications
- [ ] Scheduled processing
- [ ] Export to Excel
- [ ] Advanced filtering
- [ ] Dashboard with statistics

## 📊 Version History

### Version 1.0 (Current)
- Modern UI redesign with gradient header
- Enhanced error handling and logging
- Large response support (1000+ invoices)
- Character sanitization
- Improved connection handling
- Two new fields: `wasfatyPrescripionId` and `patientId`

---

**Built with ❤️ for efficient invoice processing**
