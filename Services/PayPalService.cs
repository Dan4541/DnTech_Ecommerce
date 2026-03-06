using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DnTech_Ecommerce.Services
{
    public class PayPalService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl;
        private readonly ILogger<PayPalService> _logger;

        public PayPalService(IConfiguration configuration, ILogger<PayPalService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            
            _clientId = configuration["PayPal:ClientId"] ?? throw new Exception("PayPal ClientId not configured");
            _clientSecret = configuration["PayPal:ClientSecret"] ?? throw new Exception("PayPal ClientSecret not configured");
            var mode = configuration["PayPal:Mode"] ?? "sandbox";
            
            _baseUrl = mode == "live" 
                ? "https://api-m.paypal.com" 
                : "https://api-m.sandbox.paypal.com";

            _logger.LogInformation($"PayPal Service initialized with mode: {mode}");
        }

        private async Task<string> GetAccessToken()
        {
            try
            {
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
                
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/oauth2/token");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to get access token. Status: {response.StatusCode}, Body: {content}");
                    throw new Exception($"PayPal authentication failed: {content}");
                }

                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
                return tokenResponse.GetProperty("access_token").GetString() ?? throw new Exception("No access token received");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting PayPal access token: {ex.Message}");
                throw;
            }
        }

        public async Task<string> CreateOrder(decimal amount, string currency = "USD", string returnUrl = "", string cancelUrl = "")
        {
            try
            {
                _logger.LogInformation($"Creating PayPal order for {amount} {currency}");
                
                var accessToken = await GetAccessToken();

                var orderRequest = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            amount = new
                            {
                                currency_code = currency,
                                value = amount.ToString("F2")
                            }
                        }
                    },
                    application_context = new
                    {
                        return_url = returnUrl,
                        cancel_url = cancelUrl
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(
                    JsonSerializer.Serialize(orderRequest),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to create order. Status: {response.StatusCode}, Body: {content}");
                    throw new Exception($"PayPal order creation failed: {content}");
                }

                _logger.LogInformation("PayPal order created successfully");
                
                var orderResponse = JsonSerializer.Deserialize<JsonElement>(content);
                var links = orderResponse.GetProperty("links");
                
                foreach (var link in links.EnumerateArray())
                {
                    if (link.GetProperty("rel").GetString() == "approve")
                    {
                        var approveUrl = link.GetProperty("href").GetString();
                        _logger.LogInformation($"Approval URL: {approveUrl}");
                        return approveUrl ?? string.Empty;
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating PayPal order: {ex.Message}");
                throw;
            }
        }

        public async Task<(bool success, string transactionId, string message)> CaptureOrder(string orderId)
        {
            try
            {
                _logger.LogInformation($"Capturing PayPal order: {orderId}");
                
                var accessToken = await GetAccessToken();

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders/{orderId}/capture");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to capture order. Status: {response.StatusCode}, Body: {content}");
                    return (false, string.Empty, $"Capture failed: {content}");
                }

                var captureResponse = JsonSerializer.Deserialize<JsonElement>(content);
                var status = captureResponse.GetProperty("status").GetString();

                if (status == "COMPLETED")
                {
                    var captureId = captureResponse
                        .GetProperty("purchase_units")[0]
                        .GetProperty("payments")
                        .GetProperty("captures")[0]
                        .GetProperty("id")
                        .GetString();

                    _logger.LogInformation($"Order captured successfully. Capture ID: {captureId}");
                    return (true, captureId ?? string.Empty, "Pago completado exitosamente");
                }

                return (false, string.Empty, $"Order status: {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error capturing order: {ex.Message}");
                return (false, string.Empty, $"Error: {ex.Message}");
            }
        }
    }
}