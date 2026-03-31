using Newtonsoft.Json;

namespace WasfatyInvoiceProcessor.Models;

public class AuthenticationRequest
{
    [JsonProperty("email")]
    public string Email { get; set; } = "aa_oraiqat@unitedpharmacy.sa";
    
    [JsonProperty("password")]
    public string Password { get; set; } = "2179372228";
}

public class AuthenticationResponse
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class InvoiceApiResponse
{
    public bool Success { get; set; }
    public InvoiceApiData? Data { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class InvoiceApiData
{
    public int Total { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public List<InvoiceError> Errors { get; set; } = new();
}

public class InvoiceError
{
    public int Index { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
