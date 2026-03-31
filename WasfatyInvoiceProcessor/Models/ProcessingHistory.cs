namespace WasfatyInvoiceProcessor.Models;

public class ProcessingHistory
{
    public int Id { get; set; }
    public DateTime ProcessingDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalInvoices { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
