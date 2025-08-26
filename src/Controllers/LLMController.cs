using Microsoft.AspNetCore.Mvc;
using VehicleMaintenanceInvoiceSystem.Services;
using VehicleMaintenanceInvoiceSystem.Attributes;

namespace VehicleMaintenanceInvoiceSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [LocalhostOnly]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class LLMController : ControllerBase
    {
        private readonly IGitHubModelsService _gitHubModelsService;
        private readonly ILogger<LLMController> _logger;

        public LLMController(
            IGitHubModelsService gitHubModelsService,
            ILogger<LLMController> logger)
        {
            _gitHubModelsService = gitHubModelsService;
            _logger = logger;
        }

        /// <summary>
        /// Test GPT-4o connectivity and capabilities
        /// </summary>
        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var testPrompt = "You are testing the connection to GPT-4o via GitHub Models. Please respond with a JSON object containing: {\"status\": \"connected\", \"model\": \"gpt-4o\", \"timestamp\": \"current_timestamp\", \"capabilities\": [\"text_processing\", \"json_output\", \"automotive_expertise\"]}";
                var testContent = "Test message for GPT-4o connection verification.";

                var result = await _gitHubModelsService.ProcessInvoiceTextAsync(testContent, testPrompt);

                if (string.IsNullOrEmpty(result))
                {
                    return BadRequest(new { error = "No response from GPT-4o", status = "failed" });
                }

                return Ok(new { 
                    status = "success", 
                    response = result,
                    message = "GPT-4o connection successful via GitHub Models"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing GPT-4o connection");
                return StatusCode(500, new { error = ex.Message, status = "failed" });
            }
        }

        /// <summary>
        /// Enhance invoice processing using GPT-4o
        /// </summary>
        [HttpPost("enhance-invoice")]
        public async Task<IActionResult> EnhanceInvoice([FromBody] EnhanceInvoiceRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.InvoiceData))
                {
                    return BadRequest(new { error = "Invoice data is required" });
                }

                var result = await _gitHubModelsService.EnhanceInvoiceDataAsync(request.InvoiceData);

                if (!result.Success)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                return Ok(new
                {
                    status = "success",
                    enhanced_data = result.EnhancedData,
                    confidence_score = result.ConfidenceScore,
                    improvements = result.Improvements,
                    processed_with = "GPT-4o via GitHub Models"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enhancing invoice with GPT-4o");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Extract part numbers from invoice text using GPT-4o
        /// </summary>
        [HttpPost("extract-parts")]
        public async Task<IActionResult> ExtractPartNumbers([FromBody] ExtractPartsRequest request)
        {
            try
            {
                var prompt = @"You are an expert automotive parts specialist. Extract ALL part numbers from the provided invoice text using these guidelines:

1. **Standard Formats**: Look for patterns like:
   - 12345-67890, ABC123, XYZ-456-789
   - Numbers with dashes, letters, or mixed alphanumeric

2. **OEM Specific**: Identify brand-specific patterns:
   - Honda: 12345-ABC-123, 90210-XXX-XXX
   - Toyota: 90210-54321, 1234567890
   - Ford: F1XZ-1234-AB, 3F2Z-XXXX-XX
   - GM: 12345678, 25912345

3. **Cross References**: Find alternative part numbers
4. **Context Clues**: Use surrounding text to validate parts vs. other numbers

Return a JSON array of objects with this structure:
{
  ""part_numbers"": [
    {
      ""number"": ""extracted_part_number"",
      ""line_text"": ""full_line_containing_part"",
      ""confidence"": 0.95,
      ""type"": ""OEM|Aftermarket|Generic"",
      ""brand"": ""Honda|Toyota|etc""
    }
  ]
}";

                var result = await _gitHubModelsService.ProcessInvoiceTextAsync(request.InvoiceText, prompt);

                return Ok(new
                {
                    status = "success",
                    result = result,
                    processed_with = "GPT-4o part number extraction"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting part numbers with GPT-4o");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Test and analyze prompt size for GPT-4o requests
        /// </summary>
        [HttpPost("test-prompt-size")]
        public IActionResult TestPromptSize([FromBody] TestPromptRequest request)
        {
            try
            {
                // Create the same prompt that's used in comprehensive processing
                var prompt = @"You are an expert automotive invoice processor. Analyze the following Azure Form Recognizer output and provide a comprehensive structured response with all required fields.

CRITICAL INSTRUCTIONS:
1. Return ONLY valid JSON - no markdown, no explanations, no extra text
2. Normalize header fields to extract key information
3. Classify each line item as: Part, Labor, Fee, Tax, or Other
4. Generate a standardized professional maintenance summary
5. Extract part numbers when present
6. Provide confidence scores for all extractions

STANDARDIZED DESCRIPTIONS:
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

Form Recognizer Data:
";

                // Create sample Form Recognizer data to test with
                var sampleFormRecognizerData = request.TestData ?? @"
{
  ""analyzeResult"": {
    ""pages"": [
      {
        ""pageNumber"": 1,
        ""angle"": 0.0,
        ""width"": 8.5,
        ""height"": 11.0,
        ""unit"": ""inch"",
        ""words"": [
          {
            ""content"": ""HONDA"",
            ""boundingBox"": [1.0, 1.0, 2.0, 1.0, 2.0, 1.5, 1.0, 1.5],
            ""confidence"": 0.999
          },
          {
            ""content"": ""SERVICE"",
            ""boundingBox"": [2.1, 1.0, 3.0, 1.0, 3.0, 1.5, 2.1, 1.5],
            ""confidence"": 0.999
          },
          {
            ""content"": ""INVOICE"",
            ""boundingBox"": [1.0, 2.0, 2.5, 2.0, 2.5, 2.5, 1.0, 2.5],
            ""confidence"": 0.999
          }
        ]
      }
    ],
    ""tables"": [
      {
        ""rowCount"": 5,
        ""columnCount"": 4,
        ""cells"": [
          {
            ""rowIndex"": 0,
            ""columnIndex"": 0,
            ""text"": ""Description"",
            ""boundingBox"": [1.0, 3.0, 3.0, 3.0, 3.0, 3.5, 1.0, 3.5],
            ""confidence"": 0.999
          },
          {
            ""rowIndex"": 1,
            ""columnIndex"": 0,
            ""text"": ""Oil Change Service"",
            ""boundingBox"": [1.0, 3.5, 3.0, 3.5, 3.0, 4.0, 1.0, 4.0],
            ""confidence"": 0.995
          },
          {
            ""rowIndex"": 1,
            ""columnIndex"": 1,
            ""text"": ""$45.00"",
            ""boundingBox"": [3.0, 3.5, 4.0, 3.5, 4.0, 4.0, 3.0, 4.0],
            ""confidence"": 0.998
          }
        ]
      }
    ],
    ""keyValuePairs"": [
      {
        ""key"": {
          ""text"": ""Invoice Number:"",
          ""boundingBox"": [1.0, 0.5, 2.5, 0.5, 2.5, 1.0, 1.0, 1.0],
          ""confidence"": 0.999
        },
        ""value"": {
          ""text"": ""INV-2025-001"",
          ""boundingBox"": [2.6, 0.5, 4.0, 0.5, 4.0, 1.0, 2.6, 1.0],
          ""confidence"": 0.998
        }
      },
      {
        ""key"": {
          ""text"": ""Vehicle ID:"",
          ""boundingBox"": [5.0, 0.5, 6.5, 0.5, 6.5, 1.0, 5.0, 1.0],
          ""confidence"": 0.999
        },
        ""value"": {
          ""text"": ""VEH001"",
          ""boundingBox"": [6.6, 0.5, 7.5, 0.5, 7.5, 1.0, 6.6, 1.0],
          ""confidence"": 0.997
        }
      }
    ],
    ""documents"": [
      {
        ""docType"": ""prebuilt-invoice"",
        ""boundingRegions"": [
          {
            ""pageNumber"": 1,
            ""boundingBox"": [0.0, 0.0, 8.5, 0.0, 8.5, 11.0, 0.0, 11.0]
          }
        ],
        ""fields"": {
          ""InvoiceTotal"": {
            ""type"": ""number"",
            ""valueNumber"": 45.00,
            ""text"": ""$45.00"",
            ""boundingBox"": [6.0, 9.0, 7.0, 9.0, 7.0, 9.5, 6.0, 9.5],
            ""confidence"": 0.998
          },
          ""InvoiceDate"": {
            ""type"": ""date"",
            ""valueDate"": ""2025-08-22"",
            ""text"": ""08/22/2025"",
            ""boundingBox"": [1.0, 1.5, 2.5, 1.5, 2.5, 2.0, 1.0, 2.0],
            ""confidence"": 0.999
          }
        },
        ""confidence"": 0.995
      }
    ]
  }
}";

                // Calculate sizes
                var promptSize = System.Text.Encoding.UTF8.GetByteCount(prompt);
                var dataSize = System.Text.Encoding.UTF8.GetByteCount(sampleFormRecognizerData);
                var totalSize = promptSize + dataSize;
                
                // Rough token estimation (1 token ≈ 4 characters for English text)
                var estimatedTokens = (prompt.Length + sampleFormRecognizerData.Length) / 4;

                return Ok(new
                {
                    status = "success",
                    analysis = new
                    {
                        prompt_size_bytes = promptSize,
                        data_size_bytes = dataSize,
                        total_size_bytes = totalSize,
                        prompt_size_chars = prompt.Length,
                        data_size_chars = sampleFormRecognizerData.Length,
                        total_size_chars = prompt.Length + sampleFormRecognizerData.Length,
                        estimated_tokens = estimatedTokens,
                        is_over_8000_tokens = estimatedTokens > 8000,
                        github_models_limit = "8000 tokens per request",
                        recommendation = estimatedTokens > 8000 ? 
                            "Prompt is too large - need to reduce Form Recognizer data or split request" :
                            "Prompt size is within acceptable limits"
                    },
                    prompt_preview = prompt.Length > 500 ? prompt.Substring(0, 500) + "..." : prompt,
                    data_preview = sampleFormRecognizerData.Length > 500 ? sampleFormRecognizerData.Substring(0, 500) + "..." : sampleFormRecognizerData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing prompt size");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class EnhanceInvoiceRequest
    {
        public string InvoiceData { get; set; } = "";
        public bool IncludePartNumbers { get; set; } = true;
        public bool IncludeClassification { get; set; } = true;
        public bool ValidateData { get; set; } = true;
    }

    public class ExtractPartsRequest
    {
        public string InvoiceText { get; set; } = "";
        public string PreferredBrand { get; set; } = "";
    }

    public class TestPromptRequest
    {
        public string TestData { get; set; } = "";
    }
}
