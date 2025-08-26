using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace VehicleMaintenanceInvoiceSystem.Services
{
    public class GitHubModelsService : IGitHubModelsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubModelsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiToken;
        private readonly string _baseUrl = "https://models.inference.ai.azure.com";

        public GitHubModelsService(
            HttpClient httpClient,
            ILogger<GitHubModelsService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _apiToken = _configuration["GitHubModels:ApiToken"] ?? "";
            
            // Configure headers for GitHub Models
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "VehicleMaintenancePlus/1.0");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> ProcessInvoiceTextAsync(string extractedText, string prompt)
        {
            try
            {
                _logger.LogInformation("Starting GPT-4o processing via GitHub Models");

                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = prompt },
                        new { role = "user", content = extractedText }
                    },
                    model = "gpt-4o",
                    temperature = 0.1,
                    max_tokens = 2000,
                    top_p = 0.9
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Making request to GitHub Models API: {Url}", $"{_baseUrl}/chat/completions");

                var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("GitHub Models API response: Status={Status}, Content={Content}", 
                                     response.StatusCode, responseContent.Length > 200 ? responseContent[..200] + "..." : responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (result.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        return choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                    }
                    
                    return "No response from GPT-4o";
                }
                else
                {
                    _logger.LogError("GitHub Models API error: {StatusCode} - {Content}", 
                                   response.StatusCode, responseContent);
                    
                    // If GitHub Models fails, return a helpful message instead of empty string
                    return $"GPT-4o API Error: {response.StatusCode}. Please check your GitHub token and try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GitHub Models API");
                return $"GPT-4o Integration Error: {ex.Message}";
            }
        }

        public async Task<InvoiceEnhancementResult> EnhanceInvoiceDataAsync(string formRecognizerData)
        {
            try
            {
                var prompt = @"You are an expert automotive invoice processor. Analyze the following invoice data extracted by Azure Form Recognizer and enhance it with the following improvements:

1. **Part Number Extraction**: Identify all automotive part numbers using these patterns:
   - Standard formats: 12345-67890, ABC123, XYZ-456-789
   - OEM numbers: Honda 12345-ABC-123, Toyota 90210-54321
   - Cross-references and alternative numbers

2. **Service Classification**: Classify each line item into categories:
   - Oil Change, Brake Service, Engine Repair, Transmission, Electrical, Tires, etc.

3. **Data Validation**: Check for:
   - Inconsistent totals, missing quantities, unclear descriptions
   - Potential OCR errors in numbers and part codes

4. **Standardization**: Normalize:
   - Part descriptions (remove extra spaces, standardize terminology)
   - Units of measure, quantities, prices
   - Service categories using consistent naming

5. **Confidence Scoring**: Provide confidence levels for extractions

Return a JSON response with enhanced data, confidence scores, and identified improvements.
Original Form Recognizer Data:";

                var result = await ProcessInvoiceTextAsync(formRecognizerData, prompt);
                
                if (string.IsNullOrEmpty(result))
                {
                    return new InvoiceEnhancementResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Failed to get response from GitHub Models API" 
                    };
                }

                // Try to parse the JSON response
                try
                {
                    var enhancedData = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
                    return new InvoiceEnhancementResult
                    {
                        Success = true,
                        EnhancedData = enhancedData ?? new(),
                        ConfidenceScore = 0.85, // Default confidence
                        Improvements = new List<string> { "GPT-4o enhanced processing applied" }
                    };
                }
                catch (JsonException)
                {
                    // If not valid JSON, treat as text response
                    return new InvoiceEnhancementResult
                    {
                        Success = true,
                        EnhancedData = new Dictionary<string, object> { { "analysis", result } },
                        ConfidenceScore = 0.75,
                        Improvements = new List<string> { "GPT-4o text analysis completed" }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enhancing invoice data with GitHub Models");
                return new InvoiceEnhancementResult 
                { 
                    Success = false, 
                    ErrorMessage = ex.Message 
                };
            }
        }
    }
}
