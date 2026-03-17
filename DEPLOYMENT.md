# Deployment Checklist

Use this checklist when deploying the Wasfaty Invoice Processor to a new machine or environment.

---

## Pre-Deployment (Before Installation)

### System Requirements
- [ ] Windows 10 or Windows 11
- [ ] .NET 8.0 Runtime installed ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- [ ] SQL Server (not Express) installed and running
- [ ] Network access to 10.10.8.182 (remote database server)
- [ ] Internet access for API calls
- [ ] At least 100 MB free disk space

### SQL Server Configuration
- [ ] SQL Server is running (check Services)
- [ ] TCP/IP protocol is enabled
- [ ] SQL Server Authentication is enabled (not just Windows Auth)
- [ ] Firewall allows SQL Server port (1433)
- [ ] SA account is enabled and password is set

### Network Verification
- [ ] Can ping 10.10.8.182 successfully
- [ ] Can access https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io
- [ ] No proxy blocking SQL Server or HTTPS traffic

---

## Installation Steps

### 1. Database Setup
- [ ] Open SQL Server Management Studio (SSMS)
- [ ] Connect to localhost
- [ ] Open `DatabaseSetup.sql`
- [ ] Execute the script (F5)
- [ ] Verify success message appears
- [ ] Verify tables were created:
  - [ ] ProcessingHistory
  - [ ] APIRequest
  - [ ] APIResponse
  - [ ] InvoiceDetails

**Verification Query:**
```sql
USE WasfatyTracker;
SELECT name FROM sys.tables;
-- Should show: ProcessingHistory, APIRequest, APIResponse, InvoiceDetails
```

### 2. Application Deployment
- [ ] Copy all files to deployment folder (e.g., `C:\WasfatyProcessor\`)
- [ ] Ensure the following files are present:
  - [ ] WasfatyInvoiceProcessor.exe
  - [ ] appsettings.json
  - [ ] All DLL files (*.dll)
  - [ ] MaterialDesignThemes files
- [ ] **Important**: Keep all files together in the same folder

### 3. Configuration
- [ ] Open `appsettings.json` in Notepad
- [ ] Verify/Update Local Database Connection:
  ```json
  "LocalConnectionString": "Server=localhost;Database=WasfatyTracker;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  ```
  - [ ] Update password if different from `123`
  - [ ] Update server name if not `localhost`

- [ ] Verify Remote Database Connection:
  ```json
  "RemoteConnectionString": "Server=10.10.8.182;Database=rmsmainprod;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;"
  ```
  - [ ] Verify IP address is correct
  - [ ] Verify credentials are correct

- [ ] Verify API Settings:
  ```json
  "Api": {
    "BaseUrl": "https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io",
    "Email": "aa_oraiqat@unitedpharmacy.sa",
    "Password": "2179372228"
  }
  ```
  - [ ] Verify API URL is correct
  - [ ] Verify credentials are correct

- [ ] Save the file

---

## Post-Deployment Testing

### 1. Initial Launch
- [ ] Double-click `WasfatyInvoiceProcessor.exe`
- [ ] Application window opens without errors
- [ ] UI loads correctly with all controls visible
- [ ] No error messages in the Activity Log

### 2. Connection Testing
- [ ] Click **"TEST CONNECTIONS"** button
- [ ] Wait for tests to complete
- [ ] Verify in Activity Log:
  - [ ] "Testing remote database connection..." → Success
  - [ ] "Testing API authentication..." → Success
  - [ ] "All connections successful!" message appears
- [ ] If any fail, review configuration and network

### 3. Test Processing (Single Date)
- [ ] Select a date that has invoice data (e.g., 2025-12-01)
- [ ] Click **"PROCESS DATE"**
- [ ] Monitor Activity Log for progress
- [ ] Verify processing completes without errors
- [ ] Check Processing History grid shows the new record
- [ ] Verify Status is "Success" or "Partial"
- [ ] Review invoice counts (Total, Created, Skipped, Failed)

### 4. Database Verification
Open SSMS and run:

```sql
USE WasfatyTracker;

-- Check processing history
SELECT TOP 5 * FROM ProcessingHistory ORDER BY ProcessingDate DESC;
-- Should show your test processing record

-- Check API requests logged
SELECT TOP 5 * FROM APIRequest ORDER BY RequestedAt DESC;
-- Should show authentication and invoice requests

-- Check API responses
SELECT TOP 5 * FROM APIResponse ORDER BY ResponseReceivedAt DESC;
-- Should show successful responses

-- Check invoice details
SELECT TOP 5 * FROM InvoiceDetails ORDER BY CreatedAt DESC;
-- Should show processed invoices
```

- [ ] All queries return data
- [ ] Data looks correct

### 5. Full Range Test (Optional)
- [ ] Set Start Date: 2025-12-01
- [ ] Set End Date: 2025-12-02 (just 2 days for testing)
- [ ] Click **"PROCESS RANGE"**
- [ ] Verify both days process successfully
- [ ] Check Processing History shows both dates

---

## Production Readiness

### Performance Check
- [ ] Single date processing completes in < 2 minutes
- [ ] No memory leaks (check Task Manager)
- [ ] Application responds normally during processing
- [ ] Log file doesn't grow excessively

### Security Check
- [ ] `appsettings.json` file permissions restricted to authorized users only
- [ ] Database SA password is strong (if different from default)
- [ ] API credentials are valid and not expired
- [ ] No sensitive data in Activity Log (passwords masked)

### Documentation
- [ ] README.md is available for reference
- [ ] QUICKSTART.md is available for users
- [ ] CONFIGURATION.md is available for admins
- [ ] Database schema documented in DatabaseSetup.sql

---

## User Training

### Basic Training (15 minutes)
- [ ] Show how to launch the application
- [ ] Demonstrate "TEST CONNECTIONS"
- [ ] Show how to process a single date
- [ ] Show how to process a date range
- [ ] Explain the Processing History grid
- [ ] Show how to read the Activity Log
- [ ] Demonstrate "GENERATE REPORT"

### Advanced Training (10 minutes)
- [ ] Explain reprocessing vs. processing
- [ ] Show how to handle errors
- [ ] Explain duplicate skipping behavior
- [ ] Show where to find detailed logs in the database
- [ ] Demonstrate configuration file editing (if applicable)

---

## Maintenance Setup

### Daily Tasks
- [ ] Document the daily processing routine:
  1. Launch application
  2. Process today's date
  3. Review results
  4. Close application

### Weekly Tasks
- [ ] Check Processing History for any failed dates
- [ ] Generate weekly summary report
- [ ] Review database size (WasfatyTracker)

### Monthly Tasks
- [ ] Archive old processing history (optional)
- [ ] Review API response logs for patterns
- [ ] Verify credentials are still valid

---

## Rollback Plan

If deployment fails, here's how to rollback:

### Emergency Rollback Steps
1. [ ] Stop the application
2. [ ] Restore previous version (if applicable)
3. [ ] Or: Drop WasfatyTracker database
4. [ ] Revert to manual Postman process

### Database Cleanup (if needed)
```sql
-- Remove all processing records
USE WasfatyTracker;
TRUNCATE TABLE InvoiceDetails;
TRUNCATE TABLE APIResponse;
TRUNCATE TABLE APIRequest;
TRUNCATE TABLE ProcessingHistory;
```

---

## Support Information

### Key Contacts
- **Database Admin**: _______________
- **Network Admin**: _______________
- **API Support**: _______________
- **Developer**: _______________

### Important Files & Locations
- **Application Folder**: _______________________
- **Database Server**: _______________________
- **Remote Database**: 10.10.8.182
- **API Endpoint**: https://uniserve-backend.victoriousglacier-19fcd426.uaenorth.azurecontainerapps.io

### Troubleshooting Resources
- [ ] README.md (detailed documentation)
- [ ] CONFIGURATION.md (settings guide)
- [ ] QUICKSTART.md (user guide)
- [ ] Activity Log in application
- [ ] SQL Server Error Logs
- [ ] Windows Event Viewer

---

## Sign-Off

### Deployed By
- **Name**: _______________
- **Date**: _______________
- **Signature**: _______________

### Verified By
- **Name**: _______________
- **Date**: _______________
- **Signature**: _______________

### Approved By
- **Name**: _______________
- **Date**: _______________
- **Signature**: _______________

---

## Deployment Status

- [ ] **Pre-Deployment Checks Complete**
- [ ] **Installation Complete**
- [ ] **Configuration Complete**
- [ ] **Testing Complete**
- [ ] **Production Ready**
- [ ] **User Training Complete**
- [ ] **Maintenance Plan Documented**
- [ ] **Support Information Documented**

---

**Deployment Date**: _______________

**Version**: 1.0.0

**Status**: ☐ Success  ☐ Failed  ☐ Partial

**Notes**:
```
_________________________________________________
_________________________________________________
_________________________________________________
_________________________________________________
```

---

*Keep this checklist for future deployments and reference.*
