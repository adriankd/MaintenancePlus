using Microsoft.AspNetCore.Mvc;
using VehicleMaintenanceInvoiceSystem.Services;

namespace VehicleMaintenanceInvoiceSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
}
