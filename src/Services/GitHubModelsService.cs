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

                // Log token estimation before API call
                var totalChars = prompt.Length + extractedText.Length;
                var estimatedTokens = totalChars / 4; // Rough estimation: 1 token ≈ 4 chars
                
                _logger.LogInformation("Request size analysis: {TotalChars} chars, ~{EstimatedTokens} tokens (GitHub Models limit: 8000 tokens)", 
                    totalChars, estimatedTokens);
                
                if (estimatedTokens > 7500) // Leave some buffer
                {
                    _logger.LogWarning("Request may exceed token limit. Consider reducing payload size.");
                }

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
                    
                    // Specific handling for common API errors
                    var errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded: GitHub Models allows 50 requests per day. Try again tomorrow or use a different API key.",
                        System.Net.HttpStatusCode.RequestEntityTooLarge => "Request payload too large: The invoice data exceeds GitHub Models' 8000 token limit. Try processing a smaller invoice.",
                        System.Net.HttpStatusCode.PaymentRequired => "GitHub Models quota exceeded: Daily API limit reached.",
                        System.Net.HttpStatusCode.Unauthorized => "Authentication failed: Invalid GitHub token or insufficient permissions.",
                        _ => $"GPT-4o API Error: {response.StatusCode}. {responseContent}"
                    };
                    
                    return errorMessage;
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

                // Check for API errors that ProcessInvoiceTextAsync returns as error strings
                if (result.StartsWith("GPT-4o API Error", StringComparison.OrdinalIgnoreCase) ||
                    result.StartsWith("GPT-4o Integration Error", StringComparison.OrdinalIgnoreCase) ||
                    result.Equals("No response from GPT-4o", StringComparison.OrdinalIgnoreCase))
                {
                    return new InvoiceEnhancementResult
                    {
                        Success = false,
                        ErrorMessage = result
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
                        ConfidenceScore = 85.0, // Use 0-100 scale to match database schema
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
                        ConfidenceScore = 75.0, // Use 0-100 scale to match database schema
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

        public async Task<ComprehensiveInvoiceProcessingResult> ProcessInvoiceComprehensivelyAsync(string formRecognizerData)
        {
            try
            {
                _logger.LogInformation("Starting comprehensive GPT-4o invoice processing");

                // Extract and simplify Form Recognizer data to reduce token count
                var simplifiedData = ExtractEssentialTextFromFormRecognizer(formRecognizerData);
                
                var prompt = @"IMPORTANT: When classifying line items, ""Cabin Filter Replacement"" and ""Engine Filter Replacement"" are LABOR (not Parts). These are services performed by technicians.

You are an expert automotive invoice processor. Analyze the following simplified invoice data and provide a comprehensive structured response with all required fields.

CRITICAL INSTRUCTIONS:
1. Return ONLY valid JSON - no markdown, no explanations, no extra text
2. Normalize header fields to extract key information
3. Classify each line item as: Part, Labor, Fee, Tax, or Other
4. Generate a standardized professional maintenance summary
5. Extract part numbers when present
6. Provide confidence scores for all extractions

*** ABSOLUTE CLASSIFICATION RULES - THESE ARE MANDATORY ***

🔧 LABOR = Any work performed by technicians (services, installations, replacements)
📦 PART = Physical items/products being sold (the actual part itself)

*** MANDATORY CLASSIFICATION DECISIONS ***
✅ ""Cabin Filter Replacement"" = Labor (NOT Part) - This is WORK to replace the filter
✅ ""Engine Filter Replacement"" = Labor (NOT Part) - This is WORK to replace the filter  
✅ ""Oil Change Service"" = Labor (NOT Part) - This is WORK to change oil
✅ ""Brake Pad Installation"" = Labor (NOT Part) - This is WORK to install pads

📦 ""Cabin Air Filter"" = Part (NOT Labor) - This is the PHYSICAL filter being sold
📦 ""Engine Oil"" = Part (NOT Labor) - This is the PHYSICAL oil being sold
📦 ""Brake Pads"" = Part (NOT Labor) - This is the PHYSICAL pads being sold

*** KEYWORD DETECTION RULES ***
IF description contains: replacement, installation, service, repair, change, maintenance, mount, install, replace → MUST BE Labor
IF description is just the part name without action words → MUST BE Part

Fee: Administrative charges (shop fees, disposal fees, environmental fees)
Tax: Government taxes and fees
Other: Miscellaneous charges that don't fit above categories

STANDARDIZED DESCRIPTIONS:
- Oil/fluid services → ""Oil Change Service""
- Multiple brake items → ""Brake System Service""
- Engine work → ""Engine Service""
- Diagnostic work → ""System Diagnostics""
- Mixed maintenance → ""Routine Maintenance""
- General service → ""General Service""

REQUIRED JSON RESPONSE FORMAT:
- Oil/fluid services → ""Oil Change Service""
- Multiple brake items → ""Brake System Service""
- Engine work → ""Engine Service""
- Diagnostic work → ""System Diagnostics""
- Mixed maintenance → ""Routine Maintenance""
- General service → ""General Service""

REQUIRED JSON RESPONSE FORMAT:
{
  ""success"": true,
  ""header"": {
    ""vehicleId"": ""extracted or normalized vehicle ID"",
    ""invoiceNumber"": ""extracted invoice number"",
    ""invoiceDate"": ""2025-08-22"",
    ""odometer"": 67890,
    ""totalCost"": 284.50,
    ""description"": ""standardized maintenance summary""
  },
  ""lineItems"": [
    {
      ""lineNumber"": 1,
      ""description"": ""original line description"",
      ""classification"": ""Part|Labor|Fee|Tax|Other"",
      ""unitCost"": 45.00,
      ""quantity"": 1,
      ""totalCost"": 45.00,
      ""partNumber"": ""extracted part number or null"",
      ""confidence"": 0.95
    }
  ],
  ""overallConfidence"": 0.92,
  ""processingNotes"": [""field mapping notes"", ""classification notes""]
}

Simplified Invoice Data:
" + simplifiedData;

                // Log the full prompt being sent to GPT-4o for debugging
                _logger.LogInformation("GPT-4o Prompt being sent (length: {PromptLength} chars):\n{Prompt}", 
                    prompt.Length, prompt);

                var result = await ProcessInvoiceTextAsync(simplifiedData, prompt);
                
                // Log the GPT-4o response for debugging classification issues
                _logger.LogInformation("GPT-4o Response received (length: {ResponseLength} chars):\n{Response}", 
                    result?.Length ?? 0, result);
                
                if (string.IsNullOrEmpty(result))
                {
                    return new ComprehensiveInvoiceProcessingResult 
                    { 
                        Success = false, 
                        ErrorMessage = "No response from GPT-4o API",
                        ProcessingMethod = "GPT4o-Failed"
                    };
                }

                // Check for rate limiting
                if (result.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || 
                    result.Contains("429", StringComparison.OrdinalIgnoreCase))
                {
                    return new ComprehensiveInvoiceProcessingResult
                    {
                        Success = false,
                        ErrorMessage = "Rate limit encountered",
                        RateLimitEncountered = true,
                        ProcessingMethod = "GPT4o-RateLimited"
                    };
                }

                // Try to parse the JSON response - handle markdown code blocks
                try
                {
                    var cleanedResult = ExtractJsonFromMarkdown(result);
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(cleanedResult);
                    
                    var comprehensiveResult = new ComprehensiveInvoiceProcessingResult
                    {
                        Success = true,
                        ProcessingMethod = "GPT4o-Enhanced"
                    };

                    // Parse header information
                    if (jsonResponse.TryGetProperty("header", out var header))
                    {
                        comprehensiveResult.VehicleId = GetStringProperty(header, "vehicleId");
                        comprehensiveResult.InvoiceNumber = GetStringProperty(header, "invoiceNumber");
                        comprehensiveResult.Description = GetStringProperty(header, "description");
                        
                        if (DateTime.TryParse(GetStringProperty(header, "invoiceDate"), out var invoiceDate))
                            comprehensiveResult.InvoiceDate = invoiceDate;
                            
                        if (int.TryParse(GetStringProperty(header, "odometer"), out var odometer))
                            comprehensiveResult.Odometer = odometer;
                            
                        if (decimal.TryParse(GetStringProperty(header, "totalCost"), out var totalCost))
                            comprehensiveResult.TotalCost = totalCost;
                    }

                    // Parse line items
                    if (jsonResponse.TryGetProperty("lineItems", out var lineItems) && lineItems.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in lineItems.EnumerateArray())
                        {
                            var lineItem = new ProcessedLineItem
                            {
                                LineNumber = GetIntProperty(item, "lineNumber"),
                                Description = GetStringProperty(item, "description") ?? "",
                                Classification = GetStringProperty(item, "classification") ?? "Other",
                                UnitCost = GetDecimalProperty(item, "unitCost"),
                                Quantity = GetDecimalProperty(item, "quantity"),
                                TotalCost = GetDecimalProperty(item, "totalCost"),
                                PartNumber = GetStringProperty(item, "partNumber"),
                                Confidence = GetDecimalProperty(item, "confidence")
                            };
                            comprehensiveResult.LineItems.Add(lineItem);
                        }
                    }

                    // Parse overall confidence
                    comprehensiveResult.OverallConfidence = GetDecimalProperty(jsonResponse, "overallConfidence");

                    // Parse processing notes
                    if (jsonResponse.TryGetProperty("processingNotes", out var notes) && notes.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var note in notes.EnumerateArray())
                        {
                            if (note.ValueKind == JsonValueKind.String)
                                comprehensiveResult.ProcessingNotes.Add(note.GetString() ?? "");
                        }
                    }

                    _logger.LogInformation("Comprehensive GPT-4o processing completed successfully. Processed {LineCount} line items with confidence {Confidence:F2}",
                        comprehensiveResult.LineItems.Count, comprehensiveResult.OverallConfidence);

                    return comprehensiveResult;
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to parse GPT-4o JSON response: {Response}", result.Length > 500 ? result[..500] + "..." : result);
                    
                    return new ComprehensiveInvoiceProcessingResult
                    {
                        Success = false,
                        ErrorMessage = $"Invalid JSON response from GPT-4o: {jsonEx.Message}",
                        ProcessingMethod = "GPT4o-JsonError",
                        ProcessingNotes = { $"Raw response: {result}" }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during comprehensive GPT-4o processing");
                return new ComprehensiveInvoiceProcessingResult 
                { 
                    Success = false, 
                    ErrorMessage = ex.Message,
                    ProcessingMethod = "GPT4o-Error"
                };
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testPrompt = "Reply with exactly: 'Connection successful'";
                var result = await ProcessInvoiceTextAsync("Test", testPrompt);
                return result.Contains("Connection successful", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                return false;
            }
        }

        private string ExtractEssentialTextFromFormRecognizer(string formRecognizerData)
        {
            try
            {
                using var document = JsonDocument.Parse(formRecognizerData);
                var root = document.RootElement;
                
                var essentialText = new StringBuilder();
                essentialText.AppendLine("=== INVOICE CONTENT ===");

                // Check if analyzeResult exists - declare it at method scope to avoid scope issues
                JsonElement analyzeResult = default;
                bool hasAnalyzeResult = root.TryGetProperty("analyzeResult", out analyzeResult);
                
                if (!hasAnalyzeResult)
                {
                    _logger.LogWarning("No analyzeResult found in Form Recognizer data, extracting Content field only");
                    
                    // Extract just the Content field which has the text content
                    if (root.TryGetProperty("Content", out var contentElement) && contentElement.ValueKind == JsonValueKind.String)
                    {
                        var content = contentElement.GetString() ?? "";
                        _logger.LogInformation("Extracted Content field: {Length} characters", content.Length);
                        return "=== EXTRACTED INVOICE TEXT ===\n" + content;
                    }
                    
                    _logger.LogWarning("No Content field found, using minimal raw data");
                    // Only include basic fields, skip the massive Pages array
                    var minimalData = new StringBuilder();
                    minimalData.AppendLine("=== MINIMAL FORM RECOGNIZER DATA ===");
                    
                    if (root.TryGetProperty("ServiceVersion", out var version))
                        minimalData.AppendLine($"ServiceVersion: {version.GetString()}");
                    if (root.TryGetProperty("ModelId", out var modelId))
                        minimalData.AppendLine($"ModelId: {modelId.GetString()}");
                    if (root.TryGetProperty("Content", out var fallbackContent))
                        minimalData.AppendLine($"Content: {fallbackContent.GetString()}");
                        
                    return minimalData.ToString();
                }

                // Extract key-value pairs (invoice header info)
                if (analyzeResult.TryGetProperty("keyValuePairs", out var keyValuePairs))
                {
                    essentialText.AppendLine("\n--- Key Information ---");
                    foreach (var kvp in keyValuePairs.EnumerateArray())
                    {
                        string key = "";
                        string value = "";
                        
                        // Extract key immediately to avoid scope issues
                        if (kvp.TryGetProperty("key", out var keyElement) &&
                            keyElement.TryGetProperty("text", out var keyTextElement))
                        {
                            key = keyTextElement.GetString() ?? "";
                        }
                            
                        // Extract value immediately to avoid scope issues
                        if (kvp.TryGetProperty("value", out var valueElement) &&
                            valueElement.TryGetProperty("text", out var valueTextElement))
                        {
                            value = valueTextElement.GetString() ?? "";
                        }
                            
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            essentialText.AppendLine($"{key}: {value}");
                        }
                    }
                }

                // Extract table data (line items)
                if (analyzeResult.TryGetProperty("tables", out var tables))
                {
                    essentialText.AppendLine("\n--- Line Items ---");
                    foreach (var table in tables.EnumerateArray())
                    {
                        if (table.TryGetProperty("cells", out var cells))
                        {
                            var currentRow = -1;
                            var rowData = new List<string>();
                            
                            foreach (var cell in cells.EnumerateArray())
                            {
                                if (cell.TryGetProperty("rowIndex", out var rowIndexElement) &&
                                    cell.TryGetProperty("text", out var cellTextElement))
                                {
                                    // Extract values immediately to avoid scope issues
                                    var row = rowIndexElement.GetInt32();
                                    var text = cellTextElement.GetString() ?? "";
                                    
                                    if (row != currentRow)
                                    {
                                        if (rowData.Count > 0)
                                        {
                                            essentialText.AppendLine(string.Join(" | ", rowData));
                                        }
                                        rowData.Clear();
                                        currentRow = row;
                                    }
                                    
                                    rowData.Add(text);
                                }
                            }
                            
                            if (rowData.Count > 0)
                            {
                                essentialText.AppendLine(string.Join(" | ", rowData));
                            }
                        }
                    }
                }

                var result = essentialText.ToString();
                
                // Log the size reduction
                var originalSize = formRecognizerData.Length;
                var reducedSize = result.Length;
                var reduction = ((double)(originalSize - reducedSize) / originalSize) * 100;
                
                _logger.LogInformation("Form Recognizer data optimized: {OriginalSize} -> {ReducedSize} chars ({Reduction:F1}% reduction)", 
                    originalSize, reducedSize, reduction);

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("Failed to parse Form Recognizer JSON, using raw data: {Error}", ex.Message);
                return formRecognizerData;
            }
        }

        private string? GetStringProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String 
                ? prop.GetString() 
                : null;
        }

        /// <summary>
        /// Extracts JSON content from markdown code blocks that GPT-4o often wraps responses in
        /// </summary>
        private string ExtractJsonFromMarkdown(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Remove markdown code block markers
            var cleaned = content.Trim();
            
            _logger.LogDebug("Extracting JSON from response starting with: {Start}", 
                cleaned.Length > 50 ? cleaned.Substring(0, 50) : cleaned);
            
            // Handle ```json ... ``` blocks
            if (cleaned.StartsWith("```json"))
            {
                var startIndex = cleaned.IndexOf('\n');
                var endIndex = cleaned.LastIndexOf("```");
                if (startIndex > 0 && endIndex > startIndex)
                {
                    var extracted = cleaned.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                    _logger.LogDebug("Extracted JSON from ```json block: {Length} chars", extracted.Length);
                    return extracted;
                }
            }
            
            // Handle ``` ... ``` blocks without language specifier
            if (cleaned.StartsWith("```") && cleaned.EndsWith("```") && cleaned.Length > 6)
            {
                var startIndex = cleaned.IndexOf('\n');
                var endIndex = cleaned.LastIndexOf("```");
                if (startIndex > 0 && endIndex > startIndex)
                {
                    var extracted = cleaned.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                    _logger.LogDebug("Extracted JSON from ``` block: {Length} chars", extracted.Length);
                    return extracted;
                }
            }
            
            // Handle single backticks or other markdown artifacts
            if (cleaned.StartsWith("`") && cleaned.EndsWith("`") && cleaned.Length > 2)
            {
                var extracted = cleaned.Substring(1, cleaned.Length - 2).Trim();
                _logger.LogDebug("Extracted JSON from ` block: {Length} chars", extracted.Length);
                return extracted;
            }
            
            _logger.LogDebug("No markdown detected, returning original content: {Length} chars", cleaned.Length);
            // Return as-is if no markdown detected
            return cleaned;
        }

        private int GetIntProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var intValue))
                    return intValue;
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parsedInt))
                    return parsedInt;
            }
            return 0;
        }

        private decimal GetDecimalProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var decimalValue))
                    return decimalValue;
                if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var parsedDecimal))
                    return parsedDecimal;
            }
            return 0;
        }
    }
}
