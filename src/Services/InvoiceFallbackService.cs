using VehicleMaintenanceInvoiceSystem.Services;
using VehicleMaintenanceInvoiceSystem.Data;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace VehicleMaintenanceInvoiceSystem.Services;

/// <summary>
/// Fallback processing service for when AI services are unavailable
/// Uses rule-based processing with in-memory keyword matching
/// </summary>
public class InvoiceFallbackService : IInvoiceFallbackService
{
    private readonly InvoiceDbContext _context;
    private readonly ILogger<InvoiceFallbackService> _logger;

    // In-memory keyword dictionaries
    private readonly Dictionary<string, string> _partKeywords;
    private readonly Dictionary<string, string> _laborKeywords;
    private readonly Dictionary<string, string> _feeKeywords;

    public InvoiceFallbackService(
        InvoiceDbContext context,
        ILogger<InvoiceFallbackService> logger)
    {
        _context = context;
        _logger = logger;

        // Initialize keyword dictionaries
        _partKeywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["oil filter"] = "Parts - Filter",
            ["air filter"] = "Parts - Filter", 
            ["cabin filter"] = "Parts - Filter",
            ["fuel filter"] = "Parts - Filter",
            ["brake pad"] = "Parts - Brake",
            ["brake disc"] = "Parts - Brake",
            ["brake rotor"] = "Parts - Brake",
            ["brake fluid"] = "Parts - Brake",
            ["tire"] = "Parts - Tire",
            ["wheel"] = "Parts - Tire",
            ["battery"] = "Parts - Electrical",
            ["alternator"] = "Parts - Electrical",
            ["starter"] = "Parts - Electrical",
            ["spark plug"] = "Parts - Engine",
            ["belt"] = "Parts - Engine",
            ["hose"] = "Parts - Engine",
            ["gasket"] = "Parts - Engine",
            ["oil"] = "Parts - Fluid",
            ["coolant"] = "Parts - Fluid",
            ["transmission fluid"] = "Parts - Fluid"
        };

        _laborKeywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["oil change"] = "Labor - Maintenance",
            ["brake service"] = "Labor - Brake", 
            ["brake repair"] = "Labor - Brake",
            ["tire rotation"] = "Labor - Tire",
            ["tire mounting"] = "Labor - Tire",
            ["wheel alignment"] = "Labor - Suspension",
            ["alignment"] = "Labor - Suspension",
            ["diagnostic"] = "Labor - Diagnostic",
            ["inspection"] = "Labor - Inspection",
            ["tune up"] = "Labor - Engine",
            ["engine service"] = "Labor - Engine",
            ["transmission service"] = "Labor - Transmission",
            ["cooling system"] = "Labor - Cooling",
            ["electrical repair"] = "Labor - Electrical",
            ["suspension repair"] = "Labor - Suspension"
        };

        _feeKeywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["shop supplies"] = "Fee - Supply",
            ["supplies"] = "Fee - Supply",
            ["disposal fee"] = "Fee - Environmental",
            ["hazmat fee"] = "Fee - Environmental",
            ["environmental fee"] = "Fee - Environmental",
            ["tax"] = "Tax - Sales",
            ["sales tax"] = "Tax - Sales",
            ["service fee"] = "Fee - Service",
            ["handling"] = "Fee - Handling"
        };
    }

    public async Task<ComprehensiveInvoiceProcessingResult> ProcessInvoiceAsync(string rawOcrData, InvoiceData invoiceData)
    {
        var result = new ComprehensiveInvoiceProcessingResult
        {
            Success = true,
            ProcessingMethod = "Fallback-RuleBased",
            RateLimitEncountered = false
        };

        try
        {
            _logger.LogInformation("Starting fallback processing for invoice with {LineCount} line items", invoiceData.LineItems.Count);

            // Process line items
            result.LineItems = new List<ProcessedLineItem>();
            
            foreach (var line in invoiceData.LineItems)
            {
                var processedLine = new ProcessedLineItem
                {
                    LineNumber = line.LineNumber,
                    Description = line.Description,
                    UnitCost = line.UnitCost,
                    Quantity = line.Quantity,
                    TotalCost = line.TotalCost,
                    Classification = ClassifyLineItem(line.Description),
                    PartNumber = ExtractPartNumber(line.Description),
                    Confidence = 0.65m // Moderate confidence for rule-based processing
                };

                result.LineItems.Add(processedLine);
            }

            // Normalize header fields
            result.VehicleId = NormalizeVehicleId(invoiceData.VehicleId);
            result.InvoiceNumber = NormalizeInvoiceNumber(invoiceData.InvoiceNumber);
            result.InvoiceDate = invoiceData.InvoiceDate;
            result.Odometer = invoiceData.Odometer;
            result.TotalCost = invoiceData.TotalCost;

            // Generate description summary
            result.Description = GenerateDescriptionSummary(result.LineItems);
            
            result.OverallConfidence = 0.65m;
            result.ProcessingNotes.Add("Processed using rule-based fallback method");
            result.ProcessingNotes.Add($"Classified {result.LineItems.Count} line items using keyword matching");

            _logger.LogInformation("Fallback processing completed successfully. Processed {LineCount} line items", result.LineItems.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback processing");
            result.Success = false;
            result.ErrorMessage = $"Fallback processing failed: {ex.Message}";
            return result;
        }
    }

    private string ClassifyLineItem(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "Other";

        var lowerDesc = description.ToLower();

        // Check parts keywords first
        foreach (var keyword in _partKeywords.Keys)
        {
            if (lowerDesc.Contains(keyword.ToLower()))
            {
                return _partKeywords[keyword];
            }
        }

        // Check labor keywords
        foreach (var keyword in _laborKeywords.Keys)
        {
            if (lowerDesc.Contains(keyword.ToLower()))
            {
                return _laborKeywords[keyword];
            }
        }

        // Check fee keywords
        foreach (var keyword in _feeKeywords.Keys)
        {
            if (lowerDesc.Contains(keyword.ToLower()))
            {
                return _feeKeywords[keyword];
            }
        }

        return "Other";
    }

    private string? ExtractPartNumber(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        // Common automotive part number patterns
        var patterns = new[]
        {
            @"\b[A-Z0-9]{3,}-[A-Z0-9]{3,}-[A-Z0-9]{2,}\b", // Honda: 15400-RTA-003
            @"\b[0-9]{5}-[0-9A-Z]{5,}\b", // Toyota: 90915-YZZD2
            @"\bF[0-9A-Z]{2}Z-[0-9A-Z]{4,}-[A-Z]{2,}\b", // Ford: F1XZ-6731-AB
            @"\b[A-Z]{2,}[0-9]{3,}[A-Z]*\b", // Generic: ACDelco PF454
            @"\b[0-9]{4,}[A-Z]{0,3}\b" // Simple numeric: 12345A
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(description, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var partNumber = match.Value.Trim();
                if (partNumber.Length >= 4 && partNumber.Length <= 25) // Reasonable length
                {
                    return partNumber;
                }
            }
        }

        return null;
    }

    private string? NormalizeVehicleId(string? vehicleId)
    {
        if (string.IsNullOrWhiteSpace(vehicleId))
            return vehicleId;

        // Remove common prefixes and clean up
        var cleaned = vehicleId.Trim().ToUpper();
        cleaned = Regex.Replace(cleaned, @"^(UNIT|VEHICLE|CAR|VIN)[:\s#]*", "", RegexOptions.IgnoreCase);
        
        // VIN validation (17 characters, alphanumeric, no I, O, Q)
        if (cleaned.Length == 17 && Regex.IsMatch(cleaned, @"^[A-HJ-NPR-Z0-9]{17}$"))
        {
            return cleaned;
        }

        return cleaned.Length > 0 ? cleaned : vehicleId;
    }

    private string? NormalizeInvoiceNumber(string? invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return invoiceNumber;

        // Remove common prefixes
        var cleaned = invoiceNumber.Trim();
        cleaned = Regex.Replace(cleaned, @"^(INVOICE|INV|RECEIPT|RCP)[:\s#-]*", "", RegexOptions.IgnoreCase);

        return cleaned.Length > 0 ? cleaned : invoiceNumber;
    }

    private string GenerateDescriptionSummary(List<ProcessedLineItem> lineItems)
    {
        if (!lineItems.Any())
            return "General automotive service";

        var categories = lineItems
            .GroupBy(l => l.Classification.Split(' ')[0]) // Get main category (Parts, Labor, etc.)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToList();

        if (categories.Count == 1)
        {
            return categories[0] switch
            {
                "Parts" => "Automotive parts replacement",
                "Labor" => "Vehicle maintenance service", 
                "Fee" => "Service fees and charges",
                "Tax" => "Tax charges",
                _ => "General automotive service"
            };
        }

        var summary = string.Join(" and ", categories.Take(2));
        return $"Automotive service including {summary.ToLower()}";
    }

    public async Task<bool> IsAvailableAsync()
    {
        // Fallback service is always available
        return await Task.FromResult(true);
    }

    public Task InitializeAsync()
    {
        // No initialization needed for in-memory service
        _logger.LogInformation("Fallback service initialized with {PartKeywords} part keywords, {LaborKeywords} labor keywords, {FeeKeywords} fee keywords",
            _partKeywords.Count, _laborKeywords.Count, _feeKeywords.Count);
        return Task.CompletedTask;
    }
}
