namespace WasfatyInvoiceProcessor.Models;

public class APIRequest
{
    public int Id { get; set; }
    public int? ProcessingHistoryId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public string RequestMethod { get; set; } = string.Empty;
    public string? RequestHeaders { get; set; }
    public string? RequestBody { get; set; }
    public DateTime RequestedAt { get; set; }
}

public class APIResponse
{
    public int Id { get; set; }
    public int APIRequestId { get; set; }
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string? ResponseHeaders { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ResponseReceivedAt { get; set; }
    public int? DurationMs { get; set; }
}
