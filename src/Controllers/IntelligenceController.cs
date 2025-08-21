using Microsoft.AspNetCore.Mvc;
using VehicleMaintenanceInvoiceSystem.Services;
using VehicleMaintenanceInvoiceSystem.Data;
using VehicleMaintenanceInvoiceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace VehicleMaintenanceInvoiceSystem.Controllers;

/// <summary>
/// API controller for intelligence features feedback collection
/// Handles user feedback for classification and normalization corrections
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IntelligenceController : ControllerBase
{
    private readonly IInvoiceIntelligenceService _intelligenceService;
    private readonly InvoiceDbContext _context;
    private readonly ILogger<IntelligenceController> _logger;

    public IntelligenceController(
        IInvoiceIntelligenceService intelligenceService,
        InvoiceDbContext context,
        ILogger<IntelligenceController> logger)
    {
        _intelligenceService = intelligenceService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Re-process an invoice with intelligence features
    /// Used when user wants to refresh classifications and normalizations
    /// </summary>
    [HttpPost("invoices/{invoiceId}/process")]
    public async Task<IActionResult> ProcessInvoiceIntelligence(int invoiceId)
    {
        try
        {
            var invoice = await _context.InvoiceHeaders
                .Include(i => i.InvoiceLines)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);

            if (invoice == null)
            {
                return NotFound(new { error = $"Invoice with ID {invoiceId} not found" });
            }

            var result = await _intelligenceService.ProcessInvoiceAsync(invoice);

            if (!result.Success)
            {
                return BadRequest(new { errors = result.Errors, warnings = result.Warnings });
            }

            // Update database with intelligence results
            await UpdateInvoiceWithIntelligenceResults(invoice, result);

            return Ok(new
            {
                success = true,
                processingTimeMs = result.ProcessingTimeMs,
                lineClassifications = result.LineClassifications,
                fieldNormalizations = result.FieldNormalizations,
                warnings = result.Warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process intelligence for invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Submit feedback for line item classification
    /// </summary>
    [HttpPost("invoices/{invoiceId}/lines/{lineId}/classification-feedback")]
    public async Task<IActionResult> SubmitClassificationFeedback(
        int invoiceId,
        int lineId,
        [FromBody] ClassificationFeedbackRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CorrectCategory))
        {
            return BadRequest(new { error = "Valid correct category is required" });
        }

        try
        {
            // Verify the line exists and belongs to the invoice
            var lineExists = await _context.InvoiceLines
                .AnyAsync(l => l.LineID == lineId && l.InvoiceID == invoiceId);

            if (!lineExists)
            {
                return NotFound(new { error = $"Invoice line {lineId} not found for invoice {invoiceId}" });
            }

            // Create feedback record
            var feedback = new ClassificationFeedback
            {
                LineID = lineId,
                InvoiceID = invoiceId,
                OriginalClassification = request.OriginalClassification ?? "",
                CorrectedClassification = request.CorrectCategory,
                OriginalConfidence = request.OriginalConfidence,
                ModelVersion = request.ClassificationVersion ?? "",
                UserID = request.UserId ?? "anonymous",
                FeedbackDate = DateTime.UtcNow,
                UserComment = request.Comments
            };

            _context.ClassificationFeedbacks.Add(feedback);

            // Update the line item with corrected classification
            var lineItem = await _context.InvoiceLines.FindAsync(lineId);
            if (lineItem != null)
            {
                lineItem.ClassifiedCategory = request.CorrectCategory;
                lineItem.ClassificationConfidence = 100; // User correction is 100% confidence
                lineItem.ClassificationMethod = "User Correction";
            }

            await _context.SaveChangesAsync();

            // Record in intelligence service for potential retraining
            await _intelligenceService.RecordClassificationFeedbackAsync(lineId, request.CorrectCategory, request.UserId ?? "anonymous");

            _logger.LogInformation("Classification feedback recorded for line {LineId}: {OriginalClassification} → {CorrectCategory}",
                lineId, request.OriginalClassification, request.CorrectCategory);

            return Ok(new { success = true, message = "Classification feedback recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record classification feedback for line {LineId}", lineId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Submit feedback for field normalization
    /// </summary>
    [HttpPost("invoices/{invoiceId}/field-normalization-feedback")]
    public async Task<IActionResult> SubmitNormalizationFeedback(
        int invoiceId,
        [FromBody] NormalizationFeedbackRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FieldName) || string.IsNullOrWhiteSpace(request.ExpectedValue))
        {
            return BadRequest(new { error = "Field name and expected value are required" });
        }

        try
        {
            // Verify the invoice exists
            var invoiceExists = await _context.InvoiceHeaders
                .AnyAsync(i => i.InvoiceID == invoiceId);

            if (!invoiceExists)
            {
                return NotFound(new { error = $"Invoice {invoiceId} not found" });
            }

            // Create feedback record
            var feedback = new FieldNormalizationFeedback
            {
                InvoiceID = invoiceId,
                OriginalLabel = request.OriginalValue ?? "",
                CurrentNormalization = request.NormalizedValue ?? "",
                ExpectedNormalization = request.ExpectedValue,
                FieldType = request.FieldName,
                NormalizationVersion = request.NormalizationVersion ?? "",
                UserID = request.UserId ?? "anonymous",
                FeedbackDate = DateTime.UtcNow,
                UserComment = request.Comments
            };

            _context.FieldNormalizationFeedbacks.Add(feedback);

            // Update the invoice header with corrected normalization if applicable
            var invoice = await _context.InvoiceHeaders.FindAsync(invoiceId);
            if (invoice != null)
            {
                switch (request.FieldName.ToLower())
                {
                    case "vehiclelabel":
                        // Apply the user-corrected value to the actual VehicleID field
                        invoice.VehicleID = request.ExpectedValue;
                        invoice.NormalizationVersion = "User Corrected";
                        break;
                    case "odometerlabel":
                        // Apply the user-corrected value to the actual Odometer field
                        if (int.TryParse(request.ExpectedValue, out var odometerValue))
                        {
                            invoice.Odometer = odometerValue;
                            invoice.NormalizationVersion = "User Corrected";
                        }
                        else
                        {
                            _logger.LogWarning("Invalid odometer value provided: {Value}", request.ExpectedValue);
                        }
                        break;
                    case "invoicelabel":
                        // Apply the user-corrected value to the actual InvoiceNumber field
                        // First check if the new invoice number already exists
                        var existingInvoice = await _context.InvoiceHeaders
                            .Where(i => i.InvoiceNumber == request.ExpectedValue && i.InvoiceID != invoiceId)
                            .FirstOrDefaultAsync();
                        
                        if (existingInvoice != null)
                        {
                            return BadRequest(new 
                            { 
                                success = false, 
                                message = $"Invoice number '{request.ExpectedValue}' already exists. Please choose a different number or resolve the conflict." 
                            });
                        }
                        
                        invoice.InvoiceNumber = request.ExpectedValue;
                        invoice.NormalizationVersion = "User Corrected";
                        break;
                }
            }

            await _context.SaveChangesAsync();

            // Record in intelligence service for potential retraining
            await _intelligenceService.RecordNormalizationFeedbackAsync(invoiceId, request.FieldName, request.ExpectedValue, request.UserId ?? "anonymous");

            _logger.LogInformation("Normalization feedback recorded for invoice {InvoiceId}, field {FieldName}: {OriginalValue} → {ExpectedValue}",
                invoiceId, request.FieldName, request.OriginalValue, request.ExpectedValue);

            return Ok(new { success = true, message = "Normalization feedback recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record normalization feedback for invoice {InvoiceId}, field {FieldName}", 
                invoiceId, request.FieldName);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get classification accuracy metrics
    /// </summary>
    [HttpGet("classification/accuracy")]
    public async Task<IActionResult> GetClassificationAccuracy([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var query = _context.ClassificationFeedbacks.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(f => f.FeedbackDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.FeedbackDate <= endDate.Value);

            var feedbacks = await query
                .GroupBy(f => f.ModelVersion)
                .Select(g => new
                {
                    Method = g.Key,
                    TotalFeedbacks = g.Count(),
                    CorrectPredictions = g.Count(f => f.OriginalClassification == f.CorrectedClassification),
                    IncorrectPredictions = g.Count(f => f.OriginalClassification != f.CorrectedClassification),
                    AverageConfidence = g.Average(f => f.OriginalConfidence)
                })
                .ToListAsync();

            object? overall = null;
            if (feedbacks.Any())
            {
                var total = feedbacks.Sum(f => f.TotalFeedbacks);
                overall = new
                {
                    TotalFeedbacks = total,
                    OverallAccuracy = total == 0 ? 0.0 : feedbacks.Sum(f => f.CorrectPredictions) / (double)total * 100,
                    AverageConfidence = feedbacks.Average(f => f.AverageConfidence)
                };
            }

            return Ok(new
            {
                dateRange = new { startDate, endDate },
                byMethod = feedbacks,
                overall = overall
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get classification accuracy metrics");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    private async Task UpdateInvoiceWithIntelligenceResults(InvoiceHeader invoice, InvoiceIntelligenceResult result)
    {
        // Update normalization results
        if (result.FieldNormalizations.Any())
        {
            invoice.NormalizationVersion = result.FieldNormalizations.First().Version;
        }

        // Update line item classifications
        foreach (var classification in result.LineClassifications)
        {
            var line = invoice.InvoiceLines?.FirstOrDefault(l => l.LineID == classification.LineId);
            if (line != null)
            {
                line.ClassifiedCategory = classification.ClassifiedCategory;
                line.ClassificationConfidence = classification.Confidence;
                line.ClassificationMethod = classification.Method;
                line.ClassificationVersion = classification.Version;
            }
        }

        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Request model for classification feedback
/// </summary>
public class ClassificationFeedbackRequest
{
    public string? OriginalClassification { get; set; }
    public string CorrectCategory { get; set; } = string.Empty;
    public decimal OriginalConfidence { get; set; }
    public string? ClassificationMethod { get; set; }
    public string? ClassificationVersion { get; set; }
    public string? UserId { get; set; }
    public string? Comments { get; set; }
}

/// <summary>
/// Request model for normalization feedback
/// </summary>
public class NormalizationFeedbackRequest
{
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? NormalizedValue { get; set; }
    public string ExpectedValue { get; set; } = string.Empty;
    public decimal OriginalConfidence { get; set; }
    public string? NormalizationMethod { get; set; }
    public string? NormalizationVersion { get; set; }
    public string? UserId { get; set; }
    public string? Comments { get; set; }
}
