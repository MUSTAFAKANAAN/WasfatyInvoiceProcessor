# Configuration Guide

## Editing Application Settings

The application uses `appsettings.json` for all configuration. This file is located next to the executable.

### Location

```
WasfatyInvoiceProcessor.exe        <- Your application
appsettings.json                    <- Configuration file (edit this)
```

### File Structure

```json
{
  "Database": {
    "LocalConnectionString": "...",
    "RemoteConnectionString": "..."
  },
  "Api": {
    "BaseUrl": "...",
    "LoginEndpoint": "...",
    "InvoiceEndpoint": "...",
    "Email": "...",
    "Password": "...",
    "TimeoutSeconds": 120
  }
}
```

---

## Database Configuration

### Local Database Connection

**Purpose**: Stores processing history, API logs, and invoice details

```json
"LocalConnectionString": "Server=localhost;Database=WasfatyTracker;User Id=sa;Password=123;TrustServerCertificate=True;"
```

**Parameters:**
- `Server`: Your local SQL Server instance (usually `localhost` or `.`)
- `Database`: Always `WasfatyTracker` (created by DatabaseSetup.sql)
- `User Id`: SQL Server username (default: `sa`)
- `Password`: SQL Server password (default: `123`)
- `TrustServerCertificate`: Set to `True` for local development

**Example - Different Local Server:**
```json
"LocalConnectionString": "Server=MYPC\\SQLEXPRESS;Database=WasfatyTracker;User Id=sa;Password=MyPassword123;TrustServerCertificate=True;"
```

**Example - Windows Authentication:**
```json
"LocalConnectionString": "Server=localhost;Database=WasfatyTracker;Integrated Security=True;TrustServerCertificate=True;"
```

### Remote Database Connection

**Purpose**: Source of invoice data to be processed

```json
"RemoteConnectionString": "Server=10.10.8.182;Database=rmsmainprod;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;"
```

**Parameters:**
- `Server`: Remote SQL Server IP or hostname
- `Database`: Always `rmsmainprod` (the production database)
- `User Id`: Remote SQL Server username
- `Password`: Remote SQL Server password
- `TrustServerCertificate`: Set to `True` to accept the server certificate

**Example - Different Remote Server:**
```json
"RemoteConnectionString": "Server=192.168.1.100;Database=rmsmainprod;User Id=dbuser;Password=SecurePass456;TrustServerCertificate=True;"
```

---

## API Configuration

### Base Configuration

```json
"Api": {
  "BaseUrl": "https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io",
  "LoginEndpoint": "/api/auth/login",
  "InvoiceEndpoint": "/api/wasfaty-tracking/invoices",
  "Email": "aa_oraiqat@unitedpharmacy.sa",
  "Password": "2179372228",
  "TimeoutSeconds": 120
}
```

### Parameters Explained

#### BaseUrl
The root URL of the Wasfaty API (without trailing slash)

**Default:**
```json
"BaseUrl": "https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io"
```

**Example - Different Environment:**
```json
"BaseUrl": "https://staging-api.example.com"
```

#### LoginEndpoint
API path for authentication (relative to BaseUrl)

**Default:**
```json
"LoginEndpoint": "/api/auth/login"
```

**Full URL becomes:**
```
https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io/api/auth/login
```

#### InvoiceEndpoint
API path for submitting invoices (relative to BaseUrl)

**Default:**
```json
"InvoiceEndpoint": "/api/wasfaty-tracking/invoices"
```

**Full URL becomes:**
```
https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io/api/wasfaty-tracking/invoices
```

#### Email & Password
Credentials for API authentication

**Default:**
```json
"Email": "aa_oraiqat@unitedpharmacy.sa",
"Password": "2179372228"
```

**Example - Different User:**
```json
"Email": "user@company.com",
"Password": "YourPassword123"
```

⚠️ **Security Note**: These credentials are stored in plain text. Keep this file secure!

#### TimeoutSeconds
Maximum time to wait for API responses (in seconds)

**Default:**
```json
"TimeoutSeconds": 120
```

This means:
- Authentication requests timeout after 120 seconds
- Invoice submission requests timeout after 120 seconds

**Recommendations:**
- **Fast network**: 60 seconds
- **Normal network**: 120 seconds (default)
- **Slow network**: 180 seconds
- **Large batches**: 300 seconds

**Example - Increase for slow connections:**
```json
"TimeoutSeconds": 300
```

---

## Complete Configuration Examples

### Example 1: Production Configuration

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

### Example 2: Test Environment

```json
{
  "Database": {
    "LocalConnectionString": "Server=.\\SQLEXPRESS;Database=WasfatyTracker;Integrated Security=True;TrustServerCertificate=True;",
    "RemoteConnectionString": "Server=test-db.company.local;Database=rmstest;User Id=testuser;Password=TestPass123;TrustServerCertificate=True;"
  },
  "Api": {
    "BaseUrl": "https://test-api.company.com",
    "LoginEndpoint": "/api/auth/login",
    "InvoiceEndpoint": "/api/wasfaty-tracking/invoices",
    "Email": "test@company.com",
    "Password": "TestPassword",
    "TimeoutSeconds": 180
  }
}
```

### Example 3: Slow Network Configuration

```json
{
  "Database": {
    "LocalConnectionString": "Server=localhost;Database=WasfatyTracker;User Id=sa;Password=123;TrustServerCertificate=True;Connection Timeout=60;",
    "RemoteConnectionString": "Server=10.10.8.182;Database=rmsmainprod;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;Connection Timeout=60;"
  },
  "Api": {
    "BaseUrl": "https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io",
    "LoginEndpoint": "/api/auth/login",
    "InvoiceEndpoint": "/api/wasfaty-tracking/invoices",
    "Email": "aa_oraiqat@unitedpharmacy.sa",
    "Password": "2179372228",
    "TimeoutSeconds": 300
  }
}
```

---

## Editing the Configuration

### Method 1: Using Notepad

1. Navigate to the application folder
2. Right-click `appsettings.json`
3. Select "Open with" → "Notepad"
4. Make your changes
5. Save the file (Ctrl+S)
6. Restart the application

### Method 2: Using Visual Studio Code

1. Open VS Code
2. File → Open File
3. Select `appsettings.json`
4. Edit with syntax highlighting
5. Save (Ctrl+S)
6. Restart the application

### Method 3: Using Any Text Editor

- Notepad++
- Sublime Text
- Visual Studio
- Any JSON editor

⚠️ **Important**: 
- Save as **UTF-8** encoding
- Keep valid **JSON format** (commas, quotes, brackets)
- **Restart the application** after changes

---

## Validating Your Configuration

### Test Your Settings

1. Launch the application
2. Click **"TEST CONNECTIONS"** button
3. Check the activity log:
   - ✅ "Testing remote database connection..."
   - ✅ "Testing API authentication..."
   - ✅ "All connections successful!"

### Common Validation Errors

**Error: "Failed to connect to database"**
- Check Server name/IP
- Verify username and password
- Ensure SQL Server is running
- Test with SQL Server Management Studio first

**Error: "Authentication failed"**
- Verify API credentials (Email/Password)
- Check BaseUrl is correct
- Ensure internet connectivity
- Try accessing the URL in a browser

**Error: "Invalid JSON"**
- Missing comma or quote
- Extra comma at end of list
- Unclosed bracket
- Use a JSON validator: jsonlint.com

---

## Security Best Practices

### Protecting Credentials

1. **Limit file access**
   - Set proper file permissions on `appsettings.json`
   - Only allow authorized users to read/write

2. **Don't commit to source control**
   - Add `appsettings.json` to `.gitignore`
   - Use `appsettings.template.json` for version control

3. **Regular password rotation**
   - Change passwords periodically
   - Update in appsettings.json

4. **Use environment-specific files**
   - `appsettings.Production.json`
   - `appsettings.Development.json`

### Creating a Template

Save this as `appsettings.template.json`:

```json
{
  "Database": {
    "LocalConnectionString": "Server=YOUR_LOCAL_SERVER;Database=WasfatyTracker;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;",
    "RemoteConnectionString": "Server=YOUR_REMOTE_SERVER;Database=rmsmainprod;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  },
  "Api": {
    "BaseUrl": "YOUR_API_BASE_URL",
    "LoginEndpoint": "/api/auth/login",
    "InvoiceEndpoint": "/api/wasfaty-tracking/invoices",
    "Email": "YOUR_EMAIL",
    "Password": "YOUR_PASSWORD",
    "TimeoutSeconds": 120
  }
}
```

Then copy and fill in actual values in `appsettings.json`.

---

## Troubleshooting Configuration

### JSON Syntax Errors

**Problem**: Application crashes on startup

**Solution**: Validate JSON syntax
```
1. Copy your appsettings.json content
2. Go to: https://jsonlint.com
3. Paste and validate
4. Fix any errors shown
```

### Connection String Issues

**Problem**: Database connection fails

**Solution**: Test connection string in SSMS
```
1. Open SQL Server Management Studio
2. Click "Connect"
3. Enter your Server name
4. Select "SQL Server Authentication"
5. Enter User ID and Password
6. Click "Connect"
7. If successful, your connection string is correct
```

### API Endpoint Issues

**Problem**: API requests fail

**Solution**: Test endpoints manually
```
1. Open Postman or browser
2. Try: https://[BaseUrl][LoginEndpoint]
3. Verify the URL works
4. Check for typos in configuration
```

---

## Advanced Configuration

### Connection String Options

Add these parameters to your connection strings as needed:

```json
"LocalConnectionString": "Server=localhost;Database=WasfatyTracker;User Id=sa;Password=123;TrustServerCertificate=True;Connection Timeout=30;Min Pool Size=5;Max Pool Size=100;MultipleActiveResultSets=True;"
```

**Common Parameters:**
- `Connection Timeout=30` - Database connection timeout (seconds)
- `Min Pool Size=5` - Minimum connection pool size
- `Max Pool Size=100` - Maximum connection pool size
- `MultipleActiveResultSets=True` - Allow multiple queries simultaneously
- `Encrypt=False` - Disable encryption (if needed)
- `Application Name=WasfatyProcessor` - Identify connection in SQL Server

### Environment Variables

You can also use environment variables (advanced):

```json
{
  "Database": {
    "LocalConnectionString": "%LOCAL_DB_CONNECTION%",
    "RemoteConnectionString": "%REMOTE_DB_CONNECTION%"
  }
}
```

Then set environment variables in Windows.

---

## Quick Reference

| Setting | Purpose | Default | When to Change |
|---------|---------|---------|----------------|
| LocalConnectionString | Local audit DB | localhost | Different SQL instance |
| RemoteConnectionString | Invoice source DB | 10.10.8.182 | Different remote server |
| BaseUrl | API root URL | Azure endpoint | Different environment |
| Email | API username | aa_oraiqat@ | Different user |
| Password | API password | 2179372228 | Security/different user |
| TimeoutSeconds | API timeout | 120 | Slow network/large batches |

---

*Need more help? Check README.md for detailed documentation.*
