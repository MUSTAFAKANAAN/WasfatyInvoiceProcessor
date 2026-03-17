using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using WasfatyInvoiceProcessor.Models;

namespace WasfatyInvoiceProcessor.Services;

public class WasfatyApiService
{
    private readonly HttpClient _httpClient;
    private readonly LocalDatabaseService _localDb;
    private readonly string _baseUrl;
    private readonly string _loginEndpoint;
    private readonly string _invoiceEndpoint;
    private readonly string _email;
    private readonly string _password;

    private string? _currentToken;
    private DateTime? _tokenExpiry;

    public WasfatyApiService(
        LocalDatabaseService localDb,
        string baseUrl,
        string loginEndpoint,
        string invoiceEndpoint,
        string email,
        string password,
        int timeoutSeconds = 300)
    {
        _localDb = localDb;
        _baseUrl = baseUrl;
        _loginEndpoint = loginEndpoint;
        _invoiceEndpoint = invoiceEndpoint;
        _email = email;
        _password = password;

        // Use HttpClientHandler to automatically decompress responses
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                     System.Net.DecompressionMethods.Deflate | 
                                     System.Net.DecompressionMethods.Brotli
        };
        
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
        
        // Set default headers to match Postman
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        _httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
        _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
    }

    // =============================================
    // Authentication
    // =============================================

    public async Task<(bool Success, string? Token, string? Error)> AuthenticateAsync(int? processingHistoryId = null)
    {
        var stopwatch = Stopwatch.StartNew();
        int? requestId = null;

        try
        {
            var url = $"{_baseUrl}{_loginEndpoint}";
            var authRequest = new AuthenticationRequest
            {
                Email = _email,
                Password = _password
            };

            var requestBody = JsonConvert.SerializeObject(authRequest);

            // Log the request
            requestId = await _localDb.LogAPIRequestAsync(
                processingHistoryId,
                "Authentication",
                url,
                "POST",
                "Content-Type: application/json",
                requestBody
            );

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            
            // Create request with proper headers
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            
            var response = await _httpClient.SendAsync(request);

            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync();

            // Log the response
            if (requestId.HasValue)
            {
                await _localDb.LogAPIResponseAsync(
                    requestId.Value,
                    (int)response.StatusCode,
                    response.IsSuccessStatusCode,
                    response.Headers.ToString(),
                    responseBody,
                    response.IsSuccessStatusCode ? null : $"Status: {response.StatusCode}",
                    (int)stopwatch.ElapsedMilliseconds
                );
            }

            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(responseBody);
                _currentToken = authResponse?.Token;
                _tokenExpiry = DateTime.UtcNow.AddHours(1); // Assuming 1 hour validity

                return (true, _currentToken, null);
            }
            else
            {
                return (false, null, $"Authentication failed with status {response.StatusCode}: {responseBody}");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            if (requestId.HasValue)
            {
                await _localDb.LogAPIResponseAsync(
                    requestId.Value,
                    0,
                    false,
                    null,
                    null,
                    ex.Message,
                    (int)stopwatch.ElapsedMilliseconds
                );
            }

            return (false, null, $"Authentication error: {ex.Message}");
        }
    }

    private async Task<bool> EnsureAuthenticatedAsync(int? processingHistoryId = null)
    {
        if (string.IsNullOrWhiteSpace(_currentToken) || 
            _tokenExpiry == null || 
            _tokenExpiry <= DateTime.UtcNow.AddMinutes(5))
        {
            var (success, token, error) = await AuthenticateAsync(processingHistoryId);
            return success;
        }

        return true;
    }

    // =============================================
    // Invoice Submission
    // =============================================

    public async Task<(bool Success, InvoiceApiResponse? Response, string? Error)> SubmitInvoicesAsync(
        List<InvoiceData> invoices, 
        int processingHistoryId)
    {
        var stopwatch = Stopwatch.StartNew();
        int? requestId = null;

        try
        {
            // Ensure we have a valid token
            if (!await EnsureAuthenticatedAsync(processingHistoryId))
            {
                return (false, null, "Failed to authenticate before submitting invoices");
            }

            var url = $"{_baseUrl}{_invoiceEndpoint}";
            var requestBody = JsonConvert.SerializeObject(invoices, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            });

            // Log the request
            requestId = await _localDb.LogAPIRequestAsync(
                processingHistoryId,
                "Invoice",
                url,
                "POST",
                $"Content-Type: application/json\nAuthorization: Bearer {_currentToken?.Substring(0, Math.Min(20, _currentToken.Length))}...",
                requestBody
            );

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            
            // Add authorization header
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentToken);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            // Read response - handle VERY large responses properly
            string responseBody;
            try
            {
                // Use a larger buffer for reading big responses (10MB)
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var memoryStream = new MemoryStream())
                {
                    // Copy with a large buffer to handle big responses (1MB chunks)
                    var buffer = new byte[1024 * 1024]; // 1MB buffer
                    int bytesRead;
                    
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    }
                    
                    // Convert to string
                    memoryStream.Position = 0;
                    using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
                    {
                        responseBody = await reader.ReadToEndAsync();
                    }
                }
                
                stopwatch.Stop();
                
                // If response is empty or whitespace only, log it
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    responseBody = "[Empty response from server]";
                }
                
                // Log response size for monitoring
                System.Diagnostics.Debug.WriteLine($"Response size: {responseBody.Length} characters, {responseBody.Length / 1024.0:F2} KB");
            }
            catch (Exception readEx)
            {
                stopwatch.Stop();
                responseBody = $"Error reading response: {readEx.Message}";
            }

            // Log the response
            if (requestId.HasValue)
            {
                await _localDb.LogAPIResponseAsync(
                    requestId.Value,
                    (int)response.StatusCode,
                    response.IsSuccessStatusCode,
                    response.Headers.ToString(),
                    responseBody,
                    response.IsSuccessStatusCode ? null : $"Status: {response.StatusCode}",
                    (int)stopwatch.ElapsedMilliseconds
                );
            }

            if (response.IsSuccessStatusCode)
            {
                // Get content type outside try block so it's available in catch
                var contentType = response.Content.Headers.ContentType?.MediaType;
                
                try
                {
                    // Log detailed information for debugging
                    System.Diagnostics.Debug.WriteLine($"Response Content-Type: {contentType}");
                    System.Diagnostics.Debug.WriteLine($"Response Length: {responseBody.Length} characters");
                    
                    // For very large responses, don't log the full content
                    if (responseBody.Length < 1000)
                    {
                        System.Diagnostics.Debug.WriteLine($"Full response: {responseBody}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"First 500 chars: {responseBody.Substring(0, Math.Min(500, responseBody.Length))}");
                        System.Diagnostics.Debug.WriteLine($"Last 500 chars: {responseBody.Substring(Math.Max(0, responseBody.Length - 500))}");
                    }
                    
                    // Trim any whitespace/control chars from beginning and end
                    var trimmedBody = responseBody.Trim();
                    
                    // Validate it looks like JSON before parsing
                    if (string.IsNullOrWhiteSpace(trimmedBody))
                    {
                        SaveDebugResponse(responseBody, "empty_response");
                        return (false, null, "Empty response from server");
                    }
                    
                    // Check if it starts with valid JSON structure
                    if (!trimmedBody.StartsWith("{") && !trimmedBody.StartsWith("["))
                    {
                        SaveDebugResponse(responseBody, "invalid_json_start");
                        return (false, null, 
                            $"Response doesn't start with JSON. First 200 chars: {trimmedBody.Substring(0, Math.Min(200, trimmedBody.Length))}");
                    }
                    
                    // Parse the JSON - use settings optimized for large responses
                    var apiResponse = JsonConvert.DeserializeObject<InvoiceApiResponse>(trimmedBody, new JsonSerializerSettings
                    {
                        MaxDepth = 128, // Allow deep nesting for large error arrays
                        DateParseHandling = DateParseHandling.None // Don't parse dates, treat as strings
                    });
                    
                    if (apiResponse == null)
                    {
                        SaveDebugResponse(responseBody, "null_deserialization");
                        return (false, null, "Failed to deserialize response - result was null");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Successfully parsed response: Total={apiResponse.Data?.Total}, Success={apiResponse.Data?.Success}, Errors={apiResponse.Data?.Errors?.Count ?? 0}");
                    
                    return (true, apiResponse, null);
                }
                catch (JsonException jsonEx)
                {
                    SaveDebugResponse(responseBody, "json_parse_error");
                    
                    return (false, null, 
                        $"Failed to parse API response: {jsonEx.Message}. " +
                        $"Response size: {responseBody.Length} chars ({responseBody.Length / 1024.0:F2} KB). " +
                        $"Status: {response.StatusCode}, " +
                        $"Content-Type: {contentType}. " +
                        $"This usually means the response was truncated or incomplete. " +
                        $"Full response saved to debug_logs folder.");
                }
                catch (Exception ex)
                {
                    SaveDebugResponse(responseBody, "general_parse_error");
                    return (false, null, 
                        $"Unexpected error parsing response: {ex.Message}. " +
                        $"Response size: {responseBody.Length} chars ({responseBody.Length / 1024.0:F2} KB). " +
                        $"Full response saved to debug_logs folder.");
                }
            }
            else
            {
                // Check if it's an authentication issue
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return (false, null, $"Unauthorized (401): Token may be invalid or expired. Response: {responseBody.Substring(0, Math.Min(500, responseBody.Length))}");
                }
                
                return (false, null, 
                    $"API request failed with status {response.StatusCode}. " +
                    $"Response: {responseBody.Substring(0, Math.Min(500, responseBody.Length))}");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            if (requestId.HasValue)
            {
                await _localDb.LogAPIResponseAsync(
                    requestId.Value,
                    0,
                    false,
                    null,
                    null,
                    ex.Message,
                    (int)stopwatch.ElapsedMilliseconds
                );
            }

            return (false, null, $"Error submitting invoices: {ex.Message}");
        }
    }

    private static string CleanJsonString(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        // Remove UTF-8 BOM if present
        if (json.StartsWith("\uFEFF"))
        {
            json = json.Substring(1);
        }

        var sb = new StringBuilder();
        foreach (char c in json)
        {
            // Only allow valid JSON characters:
            // - Standard ASCII printable (32-126): space, digits, letters, punctuation
            // - Specific JSON structure chars: {}[]":,
            // - Whitespace: space, tab, newline, carriage return
            // - Keep extended Unicode for non-English text (>= 128)
            
            if ((c >= 32 && c <= 126) ||  // Standard ASCII printable
                c == '\t' || c == '\n' || c == '\r' ||  // Whitespace
                c >= 128)  // Keep all extended Unicode (including Arabic, etc.)
            {
                // Skip specific problematic characters
                if (c == '\u2205' || c == '\'' || c == '\u2018' || c == '\u2019')
                {
                    continue;
                }
                sb.Append(c);
            }
        }
        
        // Trim any leading/trailing whitespace or control characters
        return sb.ToString().Trim();
    }
    
    private static void SaveDebugResponse(string responseBody, string prefix = "response")
    {
        try
        {
            var fileName = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var debugDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_logs");
            Directory.CreateDirectory(debugDir);
            
            var filePath = Path.Combine(debugDir, fileName);
            File.WriteAllText(filePath, responseBody, Encoding.UTF8);
            
            System.Diagnostics.Debug.WriteLine($"Debug response saved to: {filePath}");
        }
        catch
        {
            // Ignore errors in debug saving
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
