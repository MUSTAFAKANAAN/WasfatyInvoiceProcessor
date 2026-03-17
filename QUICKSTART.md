# Wasfaty Invoice Processor - Quick Start Guide

## 🚀 Get Started in 3 Steps

### Step 1: Setup Database (5 minutes)

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your local SQL Server: `localhost`
3. Open the file: `DatabaseSetup.sql`
4. Click **Execute** (F5)
5. Wait for success message: "Database setup completed successfully!"

**That's it!** Your local database is ready.

---

### Step 2: Build the Application (2 minutes)

**Option A: Using the Build Script (Easiest)**
1. Double-click `Build.bat`
2. Wait for the build to complete
3. The executable will be in the `Publish` folder

**Option B: Using Command Line**
```cmd
cd d:\WasfatyTracker
dotnet restore
dotnet build --configuration Release
```

The application will be at:
```
d:\WasfatyTracker\WasfatyInvoiceProcessor\bin\Release\net8.0-windows\WasfatyInvoiceProcessor.exe
```

---

### Step 3: Run the Application (30 seconds)

1. Navigate to the output folder (see Step 2)
2. Double-click `WasfatyInvoiceProcessor.exe`
3. Click **"TEST CONNECTIONS"** to verify everything works
4. Start processing! 🎉

---

## 📅 Your First Processing

### Process December 2025 to Today

The application is pre-configured to start from **December 1, 2025**:

1. **Launch the application**

2. **In the "Process Date Range" section:**
   - Start Date: Already set to `2025-12-01`
   - End Date: Already set to today
   - Click **"PROCESS RANGE"**

3. **Watch the magic happen:**
   - The application will process each day automatically
   - You'll see real-time progress in the activity log
   - Processing history will update as each day completes

4. **Review results:**
   - Check the Processing History grid for all processed dates
   - Click **"GENERATE REPORT"** for a summary

**That's it!** All your invoices from December 1, 2025 to today are now processed.

---

## 🎯 Daily Usage

### Process Today's Invoices

1. Open the application
2. Select today's date in the "Process Single Date" picker
3. Click **"PROCESS DATE"**
4. Done!

### Process Missing Days

1. Check the Processing History grid
2. Select the date range that's missing
3. Click **"PROCESS RANGE"**

---

## ⚙️ Default Settings

The application comes pre-configured with:

- **Start Date**: December 1, 2025
- **Local Database**: localhost (sa/123)
- **Remote Database**: 10.10.8.182 (sa/P@ssw0rd)
- **API Credentials**: Pre-configured and ready

No configuration needed! Just build and run.

---

## 🔍 What Happens During Processing?

For each date, the application:

1. ✅ **Authenticates** with the Wasfaty API
2. 📊 **Fetches invoices** from the remote database (10.10.8.182)
3. 🚀 **Submits invoices** to the API
4. 💾 **Logs everything** in your local database
5. ✨ **Shows results** in real-time

All API requests and responses are saved in your local database for audit purposes.

---

## 📊 Understanding Results

After processing, you'll see:

- **Total**: Number of invoices found
- **Created**: Successfully created in the API (✅ New invoices)
- **Skipped**: Already exist (duplicates are OK!)
- **Failed**: Had errors (check the log for details)

**Example:**
```
Result: SUCCESS
  Total Invoices: 5
  Created: 1
  Skipped: 4
  Failed: 0
  Message: Processed 5 invoices: 1 created, 4 skipped, 0 failed
```

This is normal! The API automatically skips duplicates.

---

## 🛠️ Common Scenarios

### Scenario 1: Starting Fresh (First Time)

**Goal**: Process all invoices from Dec 1, 2025 to today

```
1. Launch application
2. Click "TEST CONNECTIONS" (verify all green)
3. Click "PROCESS RANGE" (dates already set!)
4. Wait for completion (shows progress)
5. Click "GENERATE REPORT" to see summary
```

### Scenario 2: Daily Processing

**Goal**: Process today's invoices every morning

```
1. Launch application
2. Today's date is already selected
3. Click "PROCESS DATE"
4. Review results
5. Close application
```

### Scenario 3: Re-process a Date

**Goal**: Re-run processing for a specific date

```
1. Select the date
2. Click "REPROCESS DATE" (not "PROCESS DATE")
3. Confirm when asked
4. Done!
```

### Scenario 4: Fill Missing Days

**Goal**: Process days you missed

```
1. Check Processing History grid
2. Note which dates are missing
3. Set Start Date and End Date
4. Click "PROCESS RANGE"
```

---

## 🎨 UI Overview

```
┌─────────────────────────────────────────────────────────────┐
│  Wasfaty Invoice Processor                                  │
│  Automated invoice processing and API submission system     │
├─────────────────────┬───────────────────────────────────────┤
│  CONTROLS           │  PROCESSING HISTORY                   │
│                     │                                       │
│  Process Single:    │  [Grid showing all processed dates]  │
│  [Date Picker]      │   - Date, Status, Counts, Times      │
│  [PROCESS DATE]     │   - Success/Failed indicators        │
│  [REPROCESS DATE]   │   - Summary messages                 │
│                     │                                       │
│  Process Range:     │                                       │
│  [Start Date]       │                                       │
│  [End Date]         │                                       │
│  [PROCESS RANGE]    │                                       │
│  [REPROCESS RANGE]  │                                       │
│                     │                                       │
│  [TEST CONNECTIONS] │                                       │
│  [REFRESH HISTORY]  │                                       │
│  [GENERATE REPORT]  │                                       │
├─────────────────────┴───────────────────────────────────────┤
│  ⓘ Status: Ready                                            │
│  [████████████████░░░░░░░] 60%                              │
├─────────────────────────────────────────────────────────────┤
│  ACTIVITY LOG                                    [CLEAR]    │
│  [10:15:23] Application started                             │
│  [10:15:25] Testing connections...                          │
│  [10:15:26] All connections successful!                     │
│  [10:15:30] Starting processing for 2026-01-03...           │
│  [10:15:32] Found 5 invoices. Submitting to API...         │
│  [10:15:35] Processing completed                            │
└─────────────────────────────────────────────────────────────┘
```

---

## 📝 Tips

✅ **Always test connections first** - Click "TEST CONNECTIONS" when you launch

✅ **Watch the activity log** - It shows everything happening in real-time

✅ **Refresh history regularly** - Click "REFRESH HISTORY" to see latest updates

✅ **Generate reports** - Use "GENERATE REPORT" for summaries

✅ **Duplicates are OK** - The API automatically skips existing invoices

✅ **Check message column** - Shows detailed results for each processing

---

## 🆘 Need Help?

### Quick Checks

1. **Database not connecting?**
   - Verify SQL Server is running (Services > SQL Server)
   - Check username/password in appsettings.json

2. **No invoices found?**
   - That date might not have qualifying invoices
   - Check remote database has data for that date

3. **API authentication failed?**
   - Check internet connectivity
   - Verify API credentials in appsettings.json

4. **Application won't start?**
   - Install .NET 8.0 Runtime from microsoft.com/net
   - Right-click .exe > Properties > Unblock

### Still Stuck?

Check the full **README.md** file for detailed troubleshooting.

---

## 🎉 You're Ready!

That's it! You now have a powerful, automated invoice processing system.

**Next Steps:**
1. Run your first processing
2. Review the results in the Processing History
3. Generate your first report
4. Set up daily processing routine

**Happy Processing!** 🚀

---

*Version 1.0.0 | Last Updated: January 3, 2026*
