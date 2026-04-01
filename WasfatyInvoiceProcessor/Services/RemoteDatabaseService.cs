using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using WasfatyInvoiceProcessor.Models;

namespace WasfatyInvoiceProcessor.Services;

public class RemoteDatabaseService
{
    private readonly string _connectionString;

    private const int MaxRetries    = 3;
    private const int RetryDelayMs  = 3000;

    // SQL error numbers considered transient (network/connection drops)
    private static readonly HashSet<int> TransientErrors = new()
    {
        -2,    // Timeout expired
        20,    // The instance of SQL Server does not support encryption
        64,    // A connection was successfully established but then an error occurred
        233,   // The client was unable to establish a connection
        10054, // Connection forcibly closed by remote host
        10060, // Connection timed out
        10061, // Connection refused
        40613, // Database on server is not currently available
        40501, // Service is busy
        40197, // Error processing request
        49918, // Not enough resources
        4060,  // Cannot open database
        1205   // Deadlock
    };

    public RemoteDatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqlConnection CreateConnection()
    {
        // Add built-in retry for connection phase and set keepalive
        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            ConnectRetryCount    = 3,
            ConnectRetryInterval = 10,
            ConnectTimeout       = 120
        };
        return new SqlConnection(builder.ConnectionString);
    }

    public async Task<List<InvoiceData>> GetInvoicesForDateAsync(DateTime targetDate)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                return await ExecuteGetInvoicesAsync(targetDate);
            }
            catch (SqlException ex) when (attempt < MaxRetries && IsTransientError(ex))
            {
                var delay = RetryDelayMs * attempt; // 3s, 6s
                System.Diagnostics.Debug.WriteLine(
                    $"[DB] Transient error on attempt {attempt}/{MaxRetries} for {targetDate:yyyy-MM-dd}: {ex.Message}. Retrying in {delay}ms...");
                await Task.Delay(delay);
            }
        }
    }

    private static bool IsTransientError(SqlException ex) =>
        ex.Errors.Cast<SqlError>().Any(e => TransientErrors.Contains(e.Number)) ||
        ex.Message.Contains("transport-level")        ||
        ex.Message.Contains("TCP Provider")           ||
        ex.Message.Contains("forcibly closed")        ||
        ex.Message.Contains("connection attempt failed") ||
        ex.Message.Contains("connected host has failed");

    private async Task<List<InvoiceData>> ExecuteGetInvoicesAsync(DateTime targetDate)
    {
        // Fresh connection on every call (critical for retries — never reuse a broken connection)
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandTimeout = 300; // 5 minutes timeout for complex query
        
        const string sql = @"
            DECLARE @TargetDate DATE = @Date;

            ;WITH InvoiceData AS (
    SELECT 
        a.Id,
        w.Reference AS WasfatyPrescripionId,
        w.PatientId,
        w.erxid AS WasfatyErxId,
        a.Barcode AS WasfatyInvoiceReference,
        'P' + SUBSTRING(a.Barcode, 10, 3) AS Alias,
        CONVERT(VARCHAR(23), a.closedatelocaltime, 121) AS InvoiceDateTime,
        CAST(a.consumerid AS NVARCHAR(50)) AS CustomerID,
        LTRIM(RTRIM(a.consumername)) AS CustomerName,
        a.ConsumerPhoneNumber AS CustomerPhone,
        CASE 
            WHEN EXISTS (
                SELECT 1 
                FROM InvoiceItems ii
                WHERE ii.InvoiceId = a.Id
                  AND ii.ItemId IN (7574,7575,7576,7577,7578,21023,21024)
            )
            THEN 90
            ELSE 28
        END AS TreatmentDurationDays,
        CASE 
            WHEN EXISTS (
                SELECT 1 
                FROM InvoiceItems ii
                WHERE ii.InvoiceId = a.Id
                  AND ii.ItemId IN (7574,7575,7576,7577,7578,21023,21024)
            )
            THEN 73
            ELSE 23
        END AS RefillAllowedAfterDays
    FROM invoices a
    INNER JOIN ApplicationUser ON SellerId = ApplicationUser.id
    LEFT JOIN wasfaty.dbo.wasfatyapr w ON w.erxid COLLATE Arabic_100_CI_AI = WasfatyPrescripionId
    WHERE InvoiceTypeId = 8
        AND LEN(a.consumerphonenumber) = 12
        AND a.CloseDate >= @TargetDate
        AND a.CloseDate < DATEADD(DAY, 1, @TargetDate)
        AND a.netamount >= 80
),
InvoiceLines AS (
    SELECT 
        inv.Id AS InvoiceId,
        a.ItemBarCode AS ItemCode,
        ISNULL(b.Description, b.NativeName) AS Description,
        SUM(a.Quantity) AS QtyDispensed,
        1 AS IsChronicMedication,
        MAX(b.MaterialNumber) AS MaterialNumber,
        MAX(inv.WasfatyErxId) AS WasfatyErxId
    FROM InvoiceData inv
    INNER JOIN InvoiceItems a ON inv.Id = a.InvoiceId
    INNER JOIN Items b ON a.ItemId = b.Id
    GROUP BY inv.Id, a.ItemBarCode, ISNULL(b.Description, b.NativeName)
)
SELECT  
    inv.WasfatyInvoiceReference AS wasfatyInvoiceReference,
    inv.WasfatyPrescripionId AS wasfatyPrescripionId,
    inv.PatientId AS patientId,
    inv.Alias AS alias,
    inv.InvoiceDateTime AS invoiceDateTime,
    inv.CustomerName AS customerName,
    inv.CustomerPhone AS customerPhone,
    inv.CustomerID AS customerId,
    inv.TreatmentDurationDays AS treatmentDurationDays,
    inv.RefillAllowedAfterDays AS refillAllowedAfterDays,
    (
        SELECT 
            lines.ItemCode AS itemCode,
            lines.Description AS description,
            lines.QtyDispensed AS qtyDispensed,
            lines.IsChronicMedication AS isChronicMedication,
            COALESCE((
                SELECT TOP 1 CAST(JSON_VALUE(activity.value, '$.Refills') AS DECIMAL(10,2))
                FROM wasfaty.dbo.WasfatyErxJson E
                CROSS APPLY OPENJSON(E.JsonBodyWithItems, '$.Entity.Prescription.Activity') AS activity
                WHERE E.ErxId = lines.WasfatyErxId
                  AND EXISTS (
                      SELECT 1
                      FROM OPENJSON(activity.value, '$.BrandItems') AS brandItem
                      WHERE JSON_VALUE(brandItem.value, '$.ItemNumber') COLLATE Arabic_100_CI_AI = lines.MaterialNumber COLLATE Arabic_100_CI_AI
                  )
            ), 0.0) AS refills
        FROM InvoiceLines lines
        WHERE lines.InvoiceId = inv.Id
        FOR JSON PATH
    ) AS invoiceLines
FROM InvoiceData inv
FOR JSON PATH;";


        try
        {
            command.CommandText = sql;
            command.Parameters.Add(new SqlParameter("@Date", System.Data.SqlDbType.DateTime) { Value = targetDate });
            
            // Use fully async reader — avoids Task.Run sync-over-async antipattern
            // that caused TCP drops when reading large result sets on thread pool threads
            var jsonBuilder = new StringBuilder();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!await reader.IsDBNullAsync(0))
                    jsonBuilder.Append(reader.GetString(0));
            }
            var jsonResult = jsonBuilder.ToString();

            if (string.IsNullOrWhiteSpace(jsonResult))
            {
                return new List<InvoiceData>();
            }

            // Parse the JSON result
            var invoices = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonResult);
            
            if (invoices == null || !invoices.Any())
            {
                return new List<InvoiceData>();
            }

            var result = new List<InvoiceData>();

            foreach (var invoice in invoices)
            {
                var invoiceData = new InvoiceData
                {
                    WasfatyInvoiceReference = invoice["wasfatyInvoiceReference"]?.ToString() ?? "",
                    WasfatyPrescripionId = invoice["wasfatyPrescripionId"]?.ToString() ?? "",
                    PatientId = invoice["patientId"]?.ToString() ?? "",
                    Alias = invoice["alias"]?.ToString() ?? "",
                    InvoiceDateTime = invoice["invoiceDateTime"]?.ToString() ?? "",
                    CustomerName = invoice["customerName"]?.ToString() ?? "",
                    CustomerPhone = invoice["customerPhone"]?.ToString() ?? "",
                    CustomerId = invoice["customerId"]?.ToString() ?? "",
                    TreatmentDurationDays = Convert.ToInt32(invoice["treatmentDurationDays"]),
                    RefillAllowedAfterDays = Convert.ToInt32(invoice["refillAllowedAfterDays"])
                };

                // Parse invoice lines
                if (invoice.ContainsKey("invoiceLines") && invoice["invoiceLines"] != null)
                {
                    var linesJson = invoice["invoiceLines"].ToString();
                    if (!string.IsNullOrWhiteSpace(linesJson))
                    {
                        var lines = JsonConvert.DeserializeObject<List<InvoiceLine>>(linesJson);
                        if (lines != null)
                        {
                            // Sanitize descriptions to remove special characters
                            foreach (var line in lines)
                            {
                                var original = line.Description;
                                line.Description = SanitizeText(line.Description);
                                
                                // Log if sanitization changed the text
                                if (original != line.Description)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Sanitized description: '{original}' -> '{line.Description}'");
                                }
                            }
                            invoiceData.InvoiceLines = lines;
                        }
                    }
                }

                // Also sanitize customer name
                var originalName = invoiceData.CustomerName;
                invoiceData.CustomerName = SanitizeText(invoiceData.CustomerName);
                
                // Log if sanitization changed the name
                if (originalName != invoiceData.CustomerName)
                {
                    System.Diagnostics.Debug.WriteLine($"Sanitized customer name: '{originalName}' -> '{invoiceData.CustomerName}'");
                }

                result.Add(invoiceData);
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error fetching invoices for date {targetDate:yyyy-MM-dd}: {ex.Message}", ex);
        }
    }   // end ExecuteGetInvoicesAsync

    private static string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder();
        foreach (char c in text)
        {
            // Allow only:
            // - Letters (A-Z, a-z)
            // - Digits (0-9)
            // - Arabic Unicode range (0600-06FF)
            // - Safe punctuation: space, dot (.), comma (,), dash (-), slash (/)
            // 
            // EXPLICITLY EXCLUDE these special characters:
            // ( ) + = & # @ ! : ; % * < > [ ] { } | \ " ' ` ~ ? $ ^ _
            if ((c >= 'A' && c <= 'Z') ||     // Uppercase letters
                (c >= 'a' && c <= 'z') ||     // Lowercase letters
                (c >= '0' && c <= '9') ||     // Digits
                (c >= 0x0600 && c <= 0x06FF) || // Arabic characters
                c == ' ' ||                   // Space
                c == '.' ||                   // Period
                c == ',' ||                   // Comma
                c == '-' ||                   // Dash/Hyphen
                c == '/')                     // Forward slash
            {
                sb.Append(c);
            }
            // Any other character (including (, ), +, =, &, #, @, !, etc.) is removed
        }

        // Trim whitespace and remove multiple consecutive spaces
        var result = sb.ToString().Trim();
        
        // Replace multiple spaces with single space
        while (result.Contains("  "))
        {
            result = result.Replace("  ", " ");
        }

        return result;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
