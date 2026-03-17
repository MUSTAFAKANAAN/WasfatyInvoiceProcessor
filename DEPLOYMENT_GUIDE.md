# Wasfaty Invoice Processor - Deployment Guide

## 📦 Quick Answer

**YES**, you can share the `D:\WasfatyTracker\Publish` folder with your business team. It contains everything needed to run the application.

## 📋 What's Included in the Publish Folder

The `Publish` folder contains:
- ✅ `WasfatyInvoiceProcessor.exe` - The main application
- ✅ All required DLL files (libraries)
- ✅ `appsettings.json` - Configuration file
- ✅ `runtimes` folder - Platform-specific libraries

**Total Size**: ~50-60 MB

## 🚀 Deployment Steps for Business Team

### Step 1: Copy the Publish Folder

1. **Copy the entire `Publish` folder** to the target computer
2. You can rename it to something like `WasfatyInvoiceProcessor`
3. Place it anywhere (e.g., `C:\Program Files\WasfatyInvoiceProcessor` or `C:\Apps\WasfatyInvoiceProcessor`)

### Step 2: Prerequisites on Target Computer

The target computer needs:

#### A. **.NET 8.0 Runtime** (Required)

**Download Link**: https://dotnet.microsoft.com/download/dotnet/8.0

Install: **".NET Desktop Runtime 8.0"** (NOT SDK, just Runtime)

**How to check if installed:**
```cmd
dotnet --version
```
Should show version 8.0.x or higher

#### B. **SQL Server** (Required)

- **Local SQL Server** (NOT Express Edition) must be running on the computer
- **Connection**: localhost
- **Login**: sa / 123
- **Database**: WasfatyTracker (will be created using DatabaseSetup.sql)

#### C. **Network Access** (Required)

- Access to remote SQL Server: `10.10.8.182`
- Access to API endpoint: `https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io`

### Step 3: Database Setup

Before running the application:

1. **Install SQL Server** on the target computer (if not already installed)
2. **Run the database setup script**:
   - Open SQL Server Management Studio (SSMS)
   - Connect to `localhost`
   - Open `DatabaseSetup.sql` (from project root)
   - Execute the script to create `WasfatyTracker` database

### Step 4: Configuration (Optional)

The `appsettings.json` file contains all configuration. Usually, you don't need to change anything, but if needed:

```json
{
  "Database": {
    "LocalConnectionString": "Server=localhost;Database=WasfatyTracker;User Id=sa;Password=123;...",
    "RemoteConnectionString": "Server=10.10.8.182;Database=rmsmainprod;User Id=sa;Password=P@ssw0rd;..."
  },
  "Api": {
    "BaseUrl": "https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io",
    "Email": "aa_oraiqat@unitedpharmacy.sa",
    "Password": "2179372228",
    "TimeoutSeconds": 300
  }
}
```

### Step 5: Run the Application

1. Navigate to the Publish folder
2. **Double-click** `WasfatyInvoiceProcessor.exe`
3. The application should start with the modern UI
4. Click **"TEST CONNECTIONS"** to verify everything works

## 📁 Files to Share

### ✅ Required Files (Must Include)

Share the **entire Publish folder** with these contents:

```
Publish/
├── WasfatyInvoiceProcessor.exe          ⭐ Main application
├── appsettings.json                     ⭐ Configuration
├── *.dll (all DLL files)                ⭐ Required libraries
├── runtimes/                            ⭐ Platform-specific files
│   ├── win/
│   ├── win-x64/
│   ├── win-x86/
│   └── win-arm64/
├── WasfatyInvoiceProcessor.deps.json
├── WasfatyInvoiceProcessor.runtimeconfig.json
└── WasfatyInvoiceProcessor.pdb          ⚠️ Optional (for debugging)
```

### ❌ Optional Files (Can Remove Before Sharing)

You can **delete these** before sharing to reduce size:

- `debug_logs/` folder - Contains debug information
- `last_request.json` - Contains last API request (may have sensitive data)
- `WasfatyInvoiceProcessor.pdb` - Debug symbols (only needed for debugging)

### 🔒 Security Note

The `appsettings.json` contains:
- ⚠️ Database passwords
- ⚠️ API credentials

**Options:**
1. **Keep it** - Team can use the app immediately (but they see credentials)
2. **Remove it** - Team must create their own `appsettings.json` (more secure)
3. **Encrypt it** - Use a secure password manager or encryption

## 📝 Installation Instructions for Business Team

Create a simple README for them:

---

### **Quick Start Guide**

**Requirements:**
- Windows 10/11
- .NET 8.0 Runtime ([Download Here](https://dotnet.microsoft.com/download/dotnet/8.0))
- SQL Server (local)
- Network access to 10.10.8.182

**Installation:**
1. Copy the entire folder to your computer
2. Install .NET 8.0 Desktop Runtime if not installed
3. Setup the database using `DatabaseSetup.sql`
4. Run `WasfatyInvoiceProcessor.exe`

**First Time:**
1. Click "TEST CONNECTIONS" button
2. Wait for "Connected" status (green indicator)
3. Select a date and click "PROCESS DATE"

**Support:**
Contact IT team if you see connection errors.

---

## 🔄 Creating a Clean Deployment Package

To prepare a clean package for sharing:

### Option 1: Quick Cleanup (Manual)

```cmd
cd D:\WasfatyTracker\Publish
rmdir /s /q debug_logs
del last_request.json
```

### Option 2: Create ZIP Package

```cmd
cd D:\WasfatyTracker
powershell Compress-Archive -Path Publish\* -DestinationPath WasfatyInvoiceProcessor_v1.0.zip
```

Then share: `WasfatyInvoiceProcessor_v1.0.zip`

### Option 3: Use a Clean Publish Command

```cmd
cd D:\WasfatyTracker
dotnet publish WasfatyInvoiceProcessor/WasfatyInvoiceProcessor.csproj --configuration Release --output Publish --self-contained false
```

## 🆕 Updating the Application

When you make changes:

1. **Rebuild** the application:
   ```cmd
   dotnet publish WasfatyInvoiceProcessor/WasfatyInvoiceProcessor.csproj --configuration Release --output Publish
   ```

2. **Copy the Publish folder** to users

3. **Users replace** their old folder with the new one
   - ⚠️ Make sure they backup their `appsettings.json` if they customized it

## 🐛 Troubleshooting

### "The application failed to start"

**Cause**: .NET Runtime not installed  
**Solution**: Install [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### "Connection failed" errors

**Cause**: Database not accessible  
**Solution**: 
- Verify SQL Server is running
- Verify `WasfatyTracker` database exists
- Check connection strings in `appsettings.json`

### "File not found" errors

**Cause**: Incomplete file copy  
**Solution**: Copy the **entire Publish folder** with all subfolders

### Application opens then immediately closes

**Cause**: Missing configuration or database  
**Solution**: 
- Ensure `appsettings.json` exists
- Run `DatabaseSetup.sql` to create database

## 📊 Deployment Checklist

Before sharing with business team:

- [ ] Latest code is built and published
- [ ] Tested the application on your machine
- [ ] `appsettings.json` has correct settings
- [ ] Removed sensitive files (optional):
  - [ ] `debug_logs/` folder
  - [ ] `last_request.json`
  - [ ] `.pdb` files (optional)
- [ ] Created a ZIP package (optional)
- [ ] Prepared `DatabaseSetup.sql` file
- [ ] Wrote installation instructions
- [ ] Included .NET Runtime download link
- [ ] Tested on a clean machine (if possible)

## 🌐 Network Deployment (Multiple Users)

If deploying to multiple computers:

### Shared Network Drive Option

1. Place the Publish folder on a network drive (e.g., `\\server\apps\WasfatyInvoiceProcessor`)
2. Users can run directly from network: `\\server\apps\WasfatyInvoiceProcessor\WasfatyInvoiceProcessor.exe`
3. ⚠️ Each user still needs:
   - Local SQL Server with WasfatyTracker database
   - .NET 8.0 Runtime installed

### Individual Installation Option

1. Copy Publish folder to each computer: `C:\Apps\WasfatyInvoiceProcessor`
2. Each computer has its own local database
3. Create desktop shortcut for users

## 💾 Backup Important Files

Users should periodically backup:
- `WasfatyTracker` database (has processing history)
- `appsettings.json` (if customized)
- `debug_logs/` (if troubleshooting issues)

**SQL Backup Command:**
```sql
BACKUP DATABASE WasfatyTracker 
TO DISK = 'C:\Backups\WasfatyTracker_Backup.bak'
WITH FORMAT;
```

## 📦 Summary

**YES, the Publish folder is all you need!**

✅ Contains the .exe and all dependencies  
✅ Self-contained deployment  
✅ No installation required (except .NET Runtime)  
✅ Can run from any folder  
✅ Can be shared via ZIP, network drive, or USB  

**Total files to share**: Just the `Publish` folder (50-60 MB)

---

**Need help?** Check the troubleshooting section or contact your IT team.

