namespace WasfatyInvoiceProcessor.Models;

public class AppSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
}

public class DatabaseSettings
{
    public string LocalConnectionString { get; set; } = string.Empty;
    public string RemoteConnectionString { get; set; } = string.Empty;
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string LoginEndpoint { get; set; } = string.Empty;
    public string InvoiceEndpoint { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
}
