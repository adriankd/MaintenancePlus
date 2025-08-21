using VehicleMaintenanceInvoiceSystem.Models;

namespace VehicleMaintenanceInvoiceSystem.Services;

/// <summary>
/// Result of invoice intelligence processing
/// Contains both classification and normalization results
/// </summary>
public class InvoiceIntelligenceResult
{
    public List<LineItemClassificationResult> LineClassifications { get; set; } = new();
    public List<FieldNormalizationResult> FieldNormalizations { get; set; } = new();
    public bool Success { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public decimal ProcessingTimeMs { get; set; }
}

/// <summary>
/// Result for individual line item classification
/// </summary>
public class LineItemClassificationResult
{
    public int LineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ClassifiedCategory { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool WasClassified { get; set; }
}

/// <summary>
/// Result for field normalization
/// </summary>
public class FieldNormalizationResult
{
    public string FieldName { get; set; } = string.Empty;
    public string OriginalValue { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool WasNormalized { get; set; }
}

/// <summary>
/// Unified service for applying intelligence processing to invoices
/// Handles both line item classification and field normalization
/// </summary>
public interface IInvoiceIntelligenceService
{
    Task<InvoiceIntelligenceResult> ProcessInvoiceAsync(InvoiceHeader invoice);
    Task<LineItemClassificationResult> ClassifyLineItemAsync(InvoiceLine lineItem);
    Task<FieldNormalizationResult> NormalizeFieldAsync(string fieldName, string originalValue, string context = "");
    Task<bool> RecordClassificationFeedbackAsync(int invoiceLineId, string userFeedback, string userId);
    Task<bool> RecordNormalizationFeedbackAsync(int invoiceHeaderId, string fieldName, string expectedValue, string userId);
}

/// <summary>
/// Phase 2 implementation of invoice intelligence service
/// Uses rule-based classification and dictionary-based field normalization
/// </summary>
public class RuleBasedInvoiceIntelligenceService : IInvoiceIntelligenceService
{
    private readonly ILineItemClassifier _lineClassifier;
    private readonly IFieldNormalizer _fieldNormalizer;
    private readonly ILogger<RuleBasedInvoiceIntelligenceService> _logger;

    public RuleBasedInvoiceIntelligenceService(
        ILineItemClassifier lineClassifier,
        IFieldNormalizer fieldNormalizer,
        ILogger<RuleBasedInvoiceIntelligenceService> logger)
    {
        _lineClassifier = lineClassifier;
        _fieldNormalizer = fieldNormalizer;
        _logger = logger;
    }

    public async Task<InvoiceIntelligenceResult> ProcessInvoiceAsync(InvoiceHeader invoice)
    {
        if (invoice == null)
        {
            return new InvoiceIntelligenceResult
            {
                Success = false,
                Errors = { "Invoice cannot be null" }
            };
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new InvoiceIntelligenceResult { Success = true };

        try
        {
            _logger.LogInformation("Starting intelligence processing for invoice {InvoiceNumber}", invoice.InvoiceNumber);

            // Process field normalizations for header fields
            await ProcessHeaderFieldNormalizations(invoice, result);

            // Process line item classifications
            if (invoice.InvoiceLines != null && invoice.InvoiceLines.Any())
            {
                await ProcessLineItemClassifications(invoice.InvoiceLines, result);
            }
            else
            {
                result.Warnings.Add("No line items found for classification");
            }

            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Completed intelligence processing for invoice {InvoiceNumber} in {ProcessingTime}ms. " +
                "Classifications: {Classifications}, Normalizations: {Normalizations}, Warnings: {Warnings}, Errors: {Errors}",
                invoice.InvoiceNumber, result.ProcessingTimeMs, result.LineClassifications.Count, 
                result.FieldNormalizations.Count, result.Warnings.Count, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            result.Success = false;
            result.Errors.Add($"Intelligence processing failed: {ex.Message}");
            
            _logger.LogError(ex, "Failed to process invoice intelligence for invoice {InvoiceNumber}", invoice.InvoiceNumber);
            return result;
        }
    }

    private async Task ProcessHeaderFieldNormalizations(InvoiceHeader invoice, InvoiceIntelligenceResult result)
    {
        var fieldMappings = new[]
        {
            (FieldName: "VehicleLabel", OriginalValue: invoice.OriginalVehicleLabel, Context: "vehicle identifier"),
            (FieldName: "OdometerLabel", OriginalValue: invoice.OriginalOdometerLabel, Context: "odometer reading"),
            (FieldName: "InvoiceLabel", OriginalValue: invoice.OriginalInvoiceLabel, Context: "invoice number")
        };

        foreach (var mapping in fieldMappings)
        {
            if (!string.IsNullOrWhiteSpace(mapping.OriginalValue))
            {
                try
                {
                    var normalizationResult = await _fieldNormalizer.NormalizeAsync(mapping.OriginalValue, mapping.Context);
                    
                    result.FieldNormalizations.Add(new FieldNormalizationResult
                    {
                        FieldName = mapping.FieldName,
                        OriginalValue = mapping.OriginalValue,
                        NormalizedValue = normalizationResult.NormalizedLabel,
                        Confidence = normalizationResult.Confidence,
                        Method = normalizationResult.Method,
                        Version = normalizationResult.Version,
                        WasNormalized = normalizationResult.WasNormalized
                    });
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Failed to normalize field {mapping.FieldName}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to normalize field {FieldName} with value '{OriginalValue}'", 
                        mapping.FieldName, mapping.OriginalValue);
                }
            }
        }
    }

    private async Task ProcessLineItemClassifications(IEnumerable<InvoiceLine> lines, InvoiceIntelligenceResult result)
    {
        foreach (var line in lines)
        {
            try
            {
                var classificationResult = await _lineClassifier.ClassifyAsync(line.Description ?? "");
                
                result.LineClassifications.Add(new LineItemClassificationResult
                {
                    LineId = line.LineID,
                    Description = line.Description ?? "",
                    ClassifiedCategory = classificationResult.Classification,
                    Confidence = classificationResult.Confidence,
                    Method = classificationResult.Method,
                    Version = classificationResult.Version,
                    WasClassified = !string.IsNullOrWhiteSpace(classificationResult.Classification) && classificationResult.Classification != "Unknown"
                });
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Failed to classify line item {line.LineID}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to classify line item {LineId} with description '{Description}'", 
                    line.LineID, line.Description);
            }
        }
    }

    public async Task<LineItemClassificationResult> ClassifyLineItemAsync(InvoiceLine lineItem)
    {
        if (lineItem == null)
        {
            throw new ArgumentNullException(nameof(lineItem));
        }

        try
        {
            var classificationResult = await _lineClassifier.ClassifyAsync(lineItem.Description ?? "");
            
            return new LineItemClassificationResult
            {
                LineId = lineItem.LineID,
                Description = lineItem.Description ?? "",
                ClassifiedCategory = classificationResult.Classification,
                Confidence = classificationResult.Confidence,
                Method = classificationResult.Method,
                Version = classificationResult.Version,
                WasClassified = !string.IsNullOrWhiteSpace(classificationResult.Classification) && classificationResult.Classification != "Unknown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to classify line item {LineId}", lineItem.LineID);
            throw;
        }
    }

    public async Task<FieldNormalizationResult> NormalizeFieldAsync(string fieldName, string originalValue, string context = "")
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name cannot be null or empty", nameof(fieldName));
        }

        try
        {
            var normalizationResult = await _fieldNormalizer.NormalizeAsync(originalValue ?? "", context);
            
            return new FieldNormalizationResult
            {
                FieldName = fieldName,
                OriginalValue = originalValue ?? "",
                NormalizedValue = normalizationResult.NormalizedLabel,
                Confidence = normalizationResult.Confidence,
                Method = normalizationResult.Method,
                Version = normalizationResult.Version,
                WasNormalized = normalizationResult.WasNormalized
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to normalize field {FieldName}", fieldName);
            throw;
        }
    }

    public async Task<bool> RecordClassificationFeedbackAsync(int invoiceLineId, string userFeedback, string userId)
    {
        try
        {
            // TODO: Implement database persistence for classification feedback
            // This would typically create a new ClassificationFeedback record
            
            _logger.LogInformation("Recording classification feedback for line {LineId}: {Feedback} by user {UserId}", 
                invoiceLineId, userFeedback, userId);

            // Placeholder for database operation
            await Task.Delay(1); // Simulate async operation
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record classification feedback for line {LineId}", invoiceLineId);
            return false;
        }
    }

    public async Task<bool> RecordNormalizationFeedbackAsync(int invoiceHeaderId, string fieldName, string expectedValue, string userId)
    {
        try
        {
            // TODO: Implement database persistence for normalization feedback
            // This would typically create a new FieldNormalizationFeedback record
            
            _logger.LogInformation("Recording normalization feedback for invoice {InvoiceId}, field {FieldName}: {ExpectedValue} by user {UserId}", 
                invoiceHeaderId, fieldName, expectedValue, userId);

            // Placeholder for database operation
            await Task.Delay(1); // Simulate async operation
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record normalization feedback for invoice {InvoiceId}, field {FieldName}", 
                invoiceHeaderId, fieldName);
            return false;
        }
    }
}
