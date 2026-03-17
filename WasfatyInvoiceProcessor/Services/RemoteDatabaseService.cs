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

    public RemoteDatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<List<InvoiceData>> GetInvoicesForDateAsync(DateTime targetDate)
    {
        using var connection = CreateConnection();
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandTimeout = 300; // 5 minutes timeout for complex query
        
        const string sql = @"
            DECLARE @TargetDate DATE = @Date;

            ;WITH InvoiceData AS (
    SELECT 
        a.Id,
        w.Reference as WasfatyPrescripionId,
        w.PatientId,
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
        AND CAST(a.closedate AS DATE) = @TargetDate
        AND a.netamount >= 100
),
InvoiceLines AS (
    SELECT 
        inv.Id AS InvoiceId,
        a.ItemBarCode AS ItemCode,
           ISNULL(b.Description, b.NativeName) AS Description,
        SUM(a.Quantity) AS QtyDispensed,
        1 AS IsChronicMedication
    FROM InvoiceData inv
    INNER JOIN InvoiceItems a ON inv.Id = a.InvoiceId
    INNER JOIN Items b ON a.ItemId = b.Id
    GROUP BY inv.Id, a.ItemBarCode,   ISNULL(b.Description, b.NativeName)
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
            lines.IsChronicMedication AS isChronicMedication
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
            
            // Use ExecuteReader to get complete JSON result (ExecuteScalar can truncate large results)
            var jsonResult = await Task.Run(() => 
            {
                var jsonBuilder = new System.Text.StringBuilder();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            jsonBuilder.Append(reader.GetString(0));
                        }
                    }
                }
                return jsonBuilder.ToString();
            });

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
    }

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
            using var connection = CreateConnection();
            connection.Open();
            return await Task.FromResult(true);
        }
        catch
        {
            return await Task.FromResult(false);
        }
    }
}
