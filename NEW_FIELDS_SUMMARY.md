# New Fields Added - Summary

## ✅ Changes Completed

### 1. **Model Updated** (`InvoiceData.cs`)

Added two new fields to the `InvoiceData` class:

```csharp
[JsonProperty("wasfatyPrescripionId")]
public string WasfatyPrescripionId { get; set; } = string.Empty;

[JsonProperty("patientId")]
public string PatientId { get; set; } = string.Empty;
```

### 2. **SQL Query Updated** (`RemoteDatabaseService.cs`)

Updated the SQL query to include the new fields:

**In the CTE:**
```sql
WITH InvoiceData AS (
    SELECT 
        a.Id,
        a.WasfatyPrescripionId,      -- ✅ NEW
        w.PatientId,                  -- ✅ NEW
        a.Barcode AS WasfatyInvoiceReference,
        ...
```

**In the SELECT:**
```sql
SELECT 
    inv.WasfatyInvoiceReference AS wasfatyInvoiceReference,
    inv.WasfatyPrescripionId AS wasfatyPrescripionId,    -- ✅ NEW
    inv.PatientId AS patientId,                          -- ✅ NEW
    inv.Alias AS alias,
    ...
```

### 3. **Parsing Logic Updated**

Updated the C# parsing code to map the new fields:

```csharp
var invoiceData = new InvoiceData
{
    WasfatyInvoiceReference = invoice["wasfatyInvoiceReference"]?.ToString() ?? "",
    WasfatyPrescripionId = invoice["wasfatyPrescripionId"]?.ToString() ?? "",  // ✅ NEW
    PatientId = invoice["patientId"]?.ToString() ?? "",                        // ✅ NEW
    Alias = invoice["alias"]?.ToString() ?? "",
    ...
```

## 📦 JSON Output Format

The bulk invoice JSON will now include:

```json
[
  {
    "wasfatyInvoiceReference": "260102200054010096",
    "wasfatyPrescripionId": "ABC123XYZ",
    "patientId": "PAT456789",
    "alias": "P200",
    "invoiceDateTime": "2026-01-02 20:00:54.000",
    "customerName": "Ahmed Ali",
    "customerPhone": "966501234567",
    "customerId": "12345",
    "treatmentDurationDays": 28,
    "refillAllowedAfterDays": 23,
    "invoiceLines": [...]
  }
]
```

## ⚠️ To Deploy

**The application is currently running and locking the files.**

**Steps to update:**

1. **Close the application** (WasfatyInvoiceProcessor.exe)

2. **Rebuild and publish:**
   ```cmd
   cd D:\WasfatyTracker
   dotnet publish WasfatyInvoiceProcessor/WasfatyInvoiceProcessor.csproj --configuration Release --output Publish --self-contained false
   ```

3. **Restart the application**

## ✅ What Was NOT Changed

- ✅ No changes to existing logic
- ✅ No changes to database structure (only reading new fields)
- ✅ No changes to API calling logic
- ✅ No changes to error handling
- ✅ No changes to UI
- ✅ All existing functionality preserved

## 📝 Notes

- The fields are mapped from the remote database (10.10.8.182)
- `WasfatyPrescripionId` comes from `invoices.WasfatyPrescripionId`
- `PatientId` comes from `wasfaty.dbo.wasfatyapr.PatientId` via LEFT JOIN
- Both fields will be empty strings (`""`) if NULL in the database
- The API is already ready to accept these fields

## 🎯 Summary

**Fields Added:**
1. `wasfatyPrescripionId` - Prescription ID from Wasfaty
2. `patientId` - Patient ID from Wasfaty approval table

**Files Modified:**
1. `Models/InvoiceData.cs` - Added properties
2. `Services/RemoteDatabaseService.cs` - Updated SQL + parsing

**Status:** ✅ **Built successfully, ready to deploy**

Just close the app and rebuild to update the Publish folder!

