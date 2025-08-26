using VehicleMaintenanceInvoiceSystem.Models;

namespace VehicleMaintenanceInvoiceSystem.Services
{
    public interface IComprehensiveProcessingService
    {
        Task<ComprehensiveInvoiceProcessingResult> ProcessInvoiceComprehensivelyAsync(string formRecognizerJson, InvoiceData invoiceData);
    }

    public class ComprehensiveProcessingService : IComprehensiveProcessingService
    {
        private readonly IGitHubModelsService _gitHubModelsService;
        private readonly IInvoiceFallbackService _fallbackService;
        private readonly ILogger<ComprehensiveProcessingService> _logger;

        public ComprehensiveProcessingService(
            IGitHubModelsService gitHubModelsService,
            IInvoiceFallbackService fallbackService,
            ILogger<ComprehensiveProcessingService> logger)
        {
            _gitHubModelsService = gitHubModelsService;
            _fallbackService = fallbackService;
            _logger = logger;
        }

        public async Task<ComprehensiveInvoiceProcessingResult> ProcessInvoiceComprehensivelyAsync(string formRecognizerJson, InvoiceData invoiceData)
        {
            _logger.LogInformation("Starting comprehensive invoice processing");

            try
            {
                // Step 1: Attempt GPT-4o processing
                _logger.LogInformation("Attempting GPT-4o comprehensive processing");
                var gptResult = await _gitHubModelsService.ProcessInvoiceComprehensivelyAsync(formRecognizerJson);

                // Step 2: Check if GPT-4o processing was successful
                if (gptResult.Success)
                {
                    _logger.LogInformation("GPT-4o processing successful with confidence {Confidence:F2}", gptResult.OverallConfidence);
                    
                    // Fill in any missing required fields from Form Recognizer data
                    FillMissingFields(gptResult, invoiceData);
                    
                    gptResult.ProcessingNotes.Add("Primary processing completed using GPT-4o");
                    return gptResult;
                }

                // Step 3: Handle rate limiting with graceful degradation
                if (gptResult.RateLimitEncountered)
                {
                    _logger.LogWarning("GPT-4o rate limit encountered, switching to fallback processing");
                    
                    var fallbackResult = await _fallbackService.ProcessInvoiceAsync(formRecognizerJson, invoiceData);
                    if (fallbackResult.Success)
                    {
                        fallbackResult.ProcessingNotes.Add("Rate limit encountered - processed using intelligent fallback system");
                        fallbackResult.ProcessingNotes.Add($"Original GPT-4o error: {gptResult.ErrorMessage}");
                    }
                    return fallbackResult;
                }

                // Step 4: Handle other GPT-4o failures
                _logger.LogError("GPT-4o processing failed: {Error}", gptResult.ErrorMessage);
                
                var fallbackForFailure = await _fallbackService.ProcessInvoiceAsync(formRecognizerJson, invoiceData);
                if (fallbackForFailure.Success)
                {
                    fallbackForFailure.ProcessingNotes.Add("GPT-4o processing failed - processed using fallback system");
                    fallbackForFailure.ProcessingNotes.Add($"Original GPT-4o error: {gptResult.ErrorMessage}");
                }

                return fallbackForFailure;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during comprehensive processing");
                
                // Final fallback attempt
                try
                {
                    var emergencyFallback = await _fallbackService.ProcessInvoiceAsync(formRecognizerJson, invoiceData);
                    if (emergencyFallback.Success)
                    {
                        emergencyFallback.ProcessingNotes.Add("Emergency fallback processing after system error");
                        emergencyFallback.ProcessingNotes.Add($"Original error: {ex.Message}");
                    }
                    return emergencyFallback;
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Emergency fallback also failed");
                    return new ComprehensiveInvoiceProcessingResult
                    {
                        Success = false,
                        ErrorMessage = $"Both primary and fallback processing failed. Primary: {ex.Message}, Fallback: {fallbackEx.Message}",
                        ProcessingMethod = "All-Systems-Failed"
                    };
                }
            }
        }

        private void FillMissingFields(ComprehensiveInvoiceProcessingResult gptResult, InvoiceData formRecognizerData)
        {
            // Fill in any fields that GPT-4o might have missed with Form Recognizer data
            if (string.IsNullOrEmpty(gptResult.VehicleId) && !string.IsNullOrEmpty(formRecognizerData.VehicleId))
            {
                gptResult.VehicleId = formRecognizerData.VehicleId;
                gptResult.ProcessingNotes.Add("Vehicle ID filled from Form Recognizer data");
            }

            if (string.IsNullOrEmpty(gptResult.InvoiceNumber) && !string.IsNullOrEmpty(formRecognizerData.InvoiceNumber))
            {
                gptResult.InvoiceNumber = formRecognizerData.InvoiceNumber;
                gptResult.ProcessingNotes.Add("Invoice Number filled from Form Recognizer data");
            }

            if (!gptResult.InvoiceDate.HasValue && formRecognizerData.InvoiceDate.HasValue)
            {
                gptResult.InvoiceDate = formRecognizerData.InvoiceDate;
                gptResult.ProcessingNotes.Add("Invoice Date filled from Form Recognizer data");
            }

            if (!gptResult.Odometer.HasValue && formRecognizerData.Odometer.HasValue)
            {
                gptResult.Odometer = formRecognizerData.Odometer;
                gptResult.ProcessingNotes.Add("Odometer filled from Form Recognizer data");
            }

            if (!gptResult.TotalCost.HasValue && formRecognizerData.TotalCost.HasValue)
            {
                gptResult.TotalCost = formRecognizerData.TotalCost;
                gptResult.ProcessingNotes.Add("Total Cost filled from Form Recognizer data");
            }

            // Ensure we have a description
            if (string.IsNullOrEmpty(gptResult.Description))
            {
                gptResult.Description = "Automotive Service";
                gptResult.ProcessingNotes.Add("Default description applied");
            }

            // Fill in missing line items if GPT-4o missed any
            if (gptResult.LineItems.Count < formRecognizerData.LineItems.Count)
            {
                var missingLineCount = formRecognizerData.LineItems.Count - gptResult.LineItems.Count;
                gptResult.ProcessingNotes.Add($"Filled {missingLineCount} missing line items from Form Recognizer data");
                
                for (int i = gptResult.LineItems.Count; i < formRecognizerData.LineItems.Count; i++)
                {
                    var frLine = formRecognizerData.LineItems[i];
                    gptResult.LineItems.Add(new ProcessedLineItem
                    {
                        LineNumber = frLine.LineNumber,
                        Description = frLine.Description,
                        Classification = "Other",
                        UnitCost = frLine.UnitCost,
                        Quantity = frLine.Quantity,
                        TotalCost = frLine.TotalCost,
                        PartNumber = frLine.PartNumber,
                        Confidence = 0.70m // Lower confidence for Form Recognizer fallback
                    });
                }
            }
        }
    }
}
