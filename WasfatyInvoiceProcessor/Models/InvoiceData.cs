using Newtonsoft.Json;

namespace WasfatyInvoiceProcessor.Models;

public class InvoiceData
{
    [JsonProperty("wasfatyInvoiceReference")]
    public string WasfatyInvoiceReference { get; set; } = string.Empty;
    
    [JsonProperty("wasfatyPrescripionId")]
    public string WasfatyPrescripionId { get; set; } = string.Empty;
    
    [JsonProperty("patientId")]
    public string PatientId { get; set; } = string.Empty;
    
    [JsonProperty("alias")]
    public string Alias { get; set; } = string.Empty;
    
    [JsonProperty("invoiceDateTime")]
    public string InvoiceDateTime { get; set; } = string.Empty;
    
    [JsonProperty("customerName")]
    public string CustomerName { get; set; } = string.Empty;
    
    [JsonProperty("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;
    
    [JsonProperty("customerId")]
    public string CustomerId { get; set; } = string.Empty;
    
    [JsonProperty("treatmentDurationDays")]
    public int TreatmentDurationDays { get; set; }
    
    [JsonProperty("refillAllowedAfterDays")]
    public int RefillAllowedAfterDays { get; set; }
    
    [JsonProperty("invoiceLines")]
    public List<InvoiceLine> InvoiceLines { get; set; } = new();
}

public class InvoiceLine
{
    [JsonProperty("itemCode")]
    public string ItemCode { get; set; } = string.Empty;
    
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonProperty("qtyDispensed")]
    public decimal QtyDispensed { get; set; }
    
    [JsonProperty("isChronicMedication")]
    public int IsChronicMedication { get; set; }
}
