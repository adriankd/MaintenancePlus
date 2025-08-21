using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Options;
using System.Text.Json;
using VehicleMaintenanceInvoiceSystem.Models;

namespace VehicleMaintenanceInvoiceSystem.Services;

/// <summary>
/// Interface for Form Recognizer OCR operations
/// </summary>
public interface IFormRecognizerService
{
    Task<FormRecognizerResult> AnalyzeInvoiceAsync(Stream documentStream);
    Task<FormRecognizerResult> AnalyzeInvoiceFromUrlAsync(string documentUrl);
    Task<FormRecognizerResult> AnalyzeInvoiceEnhancedAsync(Stream documentStream);
}

/// <summary>
/// Result from Form Recognizer analysis
/// </summary>
public class FormRecognizerResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public InvoiceData? InvoiceData { get; set; }
    public decimal OverallConfidence { get; set; }
    public string? RawJson { get; set; }
}

/// <summary>
/// Structured invoice data extracted from Form Recognizer
/// </summary>
public class InvoiceData
{
    public string? VehicleId { get; set; }
    public int? Odometer { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? TotalPartsCost { get; set; }
    public decimal? TotalLaborCost { get; set; }
    public List<InvoiceLineData> LineItems { get; set; } = new();
}

/// <summary>
/// Individual line item data
/// </summary>
public class InvoiceLineData
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalCost { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }
    public decimal ConfidenceScore { get; set; }
}

/// <summary>
/// Service for Azure Form Recognizer operations
/// </summary>
public class FormRecognizerService : IFormRecognizerService
{
    private readonly DocumentAnalysisClient _client;
    private readonly FormRecognizerOptions _options;
    private readonly ILogger<FormRecognizerService> _logger;

    public FormRecognizerService(IOptions<FormRecognizerOptions> options, ILogger<FormRecognizerService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new DocumentAnalysisClient(new Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));
    }

    public async Task<FormRecognizerResult> AnalyzeInvoiceAsync(Stream documentStream)
    {
        try
        {
            _logger.LogInformation("Starting Form Recognizer analysis for uploaded document");

            var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, _options.ModelId, documentStream);
            var result = operation.Value;

            return ProcessAnalysisResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document with Form Recognizer");
            return new FormRecognizerResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                OverallConfidence = 0
            };
        }
    }

    public async Task<FormRecognizerResult> AnalyzeInvoiceFromUrlAsync(string documentUrl)
    {
        try
        {
            _logger.LogInformation("Starting Form Recognizer analysis for document URL: {DocumentUrl}", documentUrl);

            var operation = await _client.AnalyzeDocumentFromUriAsync(WaitUntil.Completed, _options.ModelId, new Uri(documentUrl));
            var result = operation.Value;

            return ProcessAnalysisResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document from URL with Form Recognizer: {DocumentUrl}", documentUrl);
            return new FormRecognizerResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                OverallConfidence = 0
            };
        }
    }

    public async Task<FormRecognizerResult> AnalyzeInvoiceEnhancedAsync(Stream documentStream)
    {
        _logger.LogInformation("Starting Enhanced OCR analysis with multiple model attempts");

        // Create a seekable copy of the stream for multiple attempts
        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream);
        
        // Try multiple analysis strategies in order of preference
        var strategies = new List<(string Name, Func<MemoryStream, Task<FormRecognizerResult>> Strategy)>
        {
            ("Prebuilt Invoice", AnalyzeWithPrebuiltInvoice),
            ("General Document", AnalyzeWithGeneralDocument),
            ("Read Model", AnalyzeWithReadModel)
        };

        FormRecognizerResult? bestResult = null;
        var allResults = new List<(string Strategy, FormRecognizerResult Result)>();

        foreach (var (name, strategy) in strategies)
        {
            try
            {
                _logger.LogInformation("Trying OCR strategy: {Strategy}", name);
                memoryStream.Position = 0;
                
                var result = await strategy(memoryStream);
                allResults.Add((name, result));

                if (result.Success)
                {
                    _logger.LogInformation("Strategy {Strategy} completed with confidence: {Confidence}%", 
                        name, result.OverallConfidence);

                    // Keep the best result based on confidence and completeness
                    if (bestResult == null || IsBetterResult(result, bestResult))
                    {
                        bestResult = result;
                    }

                    // If we have a high-confidence result, we can stop early
                    if (result.OverallConfidence >= 85 && result.InvoiceData?.LineItems.Count > 0)
                    {
                        _logger.LogInformation("High confidence result found with {Strategy}, stopping further attempts", name);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Strategy {Strategy} failed: {Error}", name, ex.Message);
                allResults.Add((name, new FormRecognizerResult 
                { 
                    Success = false, 
                    ErrorMessage = ex.Message, 
                    OverallConfidence = 0 
                }));
            }
        }

        if (bestResult != null)
        {
            // Enhance the best result with post-processing
            bestResult = EnhanceExtractionResult(bestResult, allResults);
            
            _logger.LogInformation("Enhanced OCR completed. Best result from strategy with {Confidence}% confidence", 
                bestResult.OverallConfidence);
            
            return bestResult;
        }

        _logger.LogError("All OCR strategies failed");
        return new FormRecognizerResult
        {
            Success = false,
            ErrorMessage = "All OCR analysis strategies failed",
            OverallConfidence = 0
        };
    }

    private FormRecognizerResult ProcessAnalysisResult(AnalyzeResult result)
    {
        try
        {
            var invoiceData = new InvoiceData();
            var confidenceScores = new List<float>();

            // Extract data from the first document (assuming single invoice per file)
            if (result.Documents.Count > 0)
            {
                var document = result.Documents[0];
                
                // Extract header fields
                if (document.Fields.TryGetValue("VehicleId", out var vehicleIdField) && vehicleIdField.FieldType == DocumentFieldType.String)
                {
                    invoiceData.VehicleId = vehicleIdField.Value.AsString();
                    confidenceScores.Add(vehicleIdField.Confidence ?? 0);
                }

                if (document.Fields.TryGetValue("InvoiceId", out var invoiceIdField) && invoiceIdField.FieldType == DocumentFieldType.String)
                {
                    invoiceData.InvoiceNumber = invoiceIdField.Value.AsString();
                    confidenceScores.Add(invoiceIdField.Confidence ?? 0);
                }

                if (document.Fields.TryGetValue("InvoiceDate", out var invoiceDateField) && invoiceDateField.FieldType == DocumentFieldType.Date)
                {
                    invoiceData.InvoiceDate = invoiceDateField.Value.AsDate().DateTime;
                    confidenceScores.Add(invoiceDateField.Confidence ?? 0);
                }

                if (document.Fields.TryGetValue("InvoiceTotal", out var totalField) && totalField.FieldType == DocumentFieldType.Currency)
                {
                    invoiceData.TotalCost = (decimal)totalField.Value.AsCurrency().Amount;
                    confidenceScores.Add(totalField.Confidence ?? 0);
                }

                // Enhanced Vehicle ID extraction from various sources
                if (string.IsNullOrEmpty(invoiceData.VehicleId))
                {
                    ExtractVehicleInformation(result, document, invoiceData, confidenceScores);
                }

                // Try to extract odometer from custom fields or text analysis
                ExtractOdometerReading(result, invoiceData, confidenceScores);

                // Extract line items
                ExtractLineItems(document, invoiceData, confidenceScores);

                // Supplement missing part numbers from table data for prebuilt invoice model
                SupplementPartNumbersFromTables(result, invoiceData, confidenceScores);

                // Calculate parts vs labor costs
                CalculatePartAndLaborTotals(invoiceData);
            }

            var overallConfidence = confidenceScores.Count > 0 ? confidenceScores.Average() * 100 : 0;

            // Serialize the raw result for storage
            var rawJson = JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            _logger.LogInformation("Form Recognizer analysis completed with overall confidence: {Confidence}%", overallConfidence);

            return new FormRecognizerResult
            {
                Success = true,
                InvoiceData = invoiceData,
                OverallConfidence = (decimal)overallConfidence,
                RawJson = rawJson
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Form Recognizer analysis result");
            return new FormRecognizerResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                OverallConfidence = 0
            };
        }
    }

    private void ExtractOdometerReading(AnalyzeResult result, InvoiceData invoiceData, List<float> confidenceScores)
    {
        // Look for odometer/mileage keywords in the extracted text
        var odometerKeywords = new[] { "odometer", "mileage", "miles", "km", "kilometers" };
        
        foreach (var page in result.Pages)
        {
            foreach (var line in page.Lines)
            {
                var text = line.Content.ToLowerInvariant();
                if (odometerKeywords.Any(keyword => text.Contains(keyword)))
                {
                    // Try to extract number from the line - handle comma-separated numbers
                    // First try to extract numbers with commas (e.g., 67,890)
                    var commaNumbers = System.Text.RegularExpressions.Regex.Matches(text, @"\d{1,3}(?:,\d{3})+")
                        .Cast<System.Text.RegularExpressions.Match>()
                        .Select(m => m.Value)
                        .ToList();

                    if (commaNumbers.Any())
                    {
                        var numberString = commaNumbers.First().Replace(",", "");
                        if (int.TryParse(numberString, out var odometerWithComma))
                        {
                            invoiceData.Odometer = odometerWithComma;
                            // Note: Confidence score for odometer extraction from text is not available
                            break;
                        }
                    }

                    // Fallback to regular numbers without commas (at least 3 digits)
                    var numbers = System.Text.RegularExpressions.Regex.Matches(text, @"\d+")
                        .Cast<System.Text.RegularExpressions.Match>()
                        .Select(m => m.Value)
                        .Where(s => s.Length >= 3) // Assume odometer has at least 3 digits
                        .ToList();

                    if (numbers.Any() && int.TryParse(numbers.First(), out var odometer))
                    {
                        invoiceData.Odometer = odometer;
                        // Note: Confidence score for odometer extraction from text is not available
                        break;
                    }
                }
            }
            if (invoiceData.Odometer.HasValue) break;
        }
    }

    private void ExtractVehicleInformation(AnalyzeResult result, AnalyzedDocument document, InvoiceData invoiceData, List<float> confidenceScores)
    {
        // Add debugging to see all available fields
        _logger.LogInformation("Available document fields: {FieldNames}", string.Join(", ", document.Fields.Keys));
        
        // First, try to find vehicle information in standard invoice fields
        TryExtractVehicleFromCustomerFields(document, invoiceData, confidenceScores);
        
        // If not found, search through all extracted text
        if (string.IsNullOrEmpty(invoiceData.VehicleId))
        {
            TryExtractVehicleFromText(result, invoiceData, confidenceScores);
        }
        
        // Clean and validate the extracted vehicle ID
        if (!string.IsNullOrEmpty(invoiceData.VehicleId))
        {
            invoiceData.VehicleId = CleanVehicleId(invoiceData.VehicleId);
        }
    }

    private void TryExtractVehicleFromCustomerFields(AnalyzedDocument document, InvoiceData invoiceData, List<float> confidenceScores)
    {
        // First, check for explicit Vehicle ID fields (highest priority)
        var vehicleIdFields = new[] { "Vehicle ID", "VehicleID", "Vehicle Id", "VehicleId", "VEHICLE ID", "Vehicle Number", "VehicleNumber", "Unit ID", "UnitID", "Fleet ID", "FleetID" };
        
        foreach (var fieldName in vehicleIdFields)
        {
            if (document.Fields.TryGetValue(fieldName, out var field) && field.FieldType == DocumentFieldType.String)
            {
                var fieldValue = field.Value.AsString();
                if (!string.IsNullOrWhiteSpace(fieldValue))
                {
                    var cleanedId = CleanVehicleId(fieldValue);
                    if (!string.IsNullOrEmpty(cleanedId))
                    {
                        invoiceData.VehicleId = cleanedId;
                        confidenceScores.Add(field.Confidence ?? 0.9f); // High confidence for explicit fields
                        _logger.LogInformation("Vehicle ID found in explicit field {FieldName}: {VehicleId}", fieldName, cleanedId);
                        return;
                    }
                }
            }
        }

        // Second, check all available fields for vehicle patterns (but exclude invoice and vendor fields)
        var excludeFields = new[] { "VendorName", "CompanyName", "BusinessName", "ServiceProvider", "InvoiceFrom", 
                                   "InvoiceNumber", "InvoiceId", "InvoiceDate", "Invoice", "InvoiceTotal", "Total", 
                                   "TotalAmount", "Amount", "AmountDue", "DueDate", "Date" };
        
        _logger.LogInformation("Searching through document fields for vehicle ID, excluding: {ExcludedFields}", string.Join(", ", excludeFields));
        
        foreach (var fieldPair in document.Fields)
        {
            var fieldName = fieldPair.Key;
            var field = fieldPair.Value;
            
            // Skip excluded fields that are likely to contain invoice numbers or business names
            if (excludeFields.Any(exclude => fieldName.Contains(exclude, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Skipping field {FieldName} - matches exclusion pattern", fieldName);
                continue;
            }
                
            if (field.FieldType == DocumentFieldType.String)
            {
                var fieldValue = field.Value.AsString();
                _logger.LogDebug("Checking field {FieldName}: {Value}", fieldName, fieldValue);
                
                var vehicleId = ExtractVehicleIdFromText(fieldValue, isExplicitSearch: false);
                
                if (!string.IsNullOrEmpty(vehicleId))
                {
                    invoiceData.VehicleId = vehicleId;
                    confidenceScores.Add(field.Confidence ?? 0.6f);
                    _logger.LogInformation("Vehicle ID found in field {FieldName}: {VehicleId}", fieldName, vehicleId);
                    return;
                }
            }
        }
    }

    private void TryExtractVehicleFromText(AnalyzeResult result, InvoiceData invoiceData, List<float> confidenceScores)
    {
        // Search through all extracted text for vehicle ID patterns, prioritizing lines that contain vehicle-related keywords
        foreach (var page in result.Pages)
        {
            // First pass: Look for lines that explicitly mention vehicle ID
            foreach (var line in page.Lines)
            {
                var lineText = line.Content;
                if (ContainsVehicleKeywords(lineText))
                {
                    var vehicleId = ExtractVehicleIdFromText(lineText, isExplicitSearch: true);
                    if (!string.IsNullOrEmpty(vehicleId))
                    {
                        invoiceData.VehicleId = vehicleId;
                        confidenceScores.Add(0.8f); // High confidence for explicit vehicle lines
                        _logger.LogInformation("Vehicle ID found in explicit vehicle line: {VehicleId}", vehicleId);
                        return;
                    }
                }
            }
            
            // Second pass: General pattern search (lower confidence)
            foreach (var line in page.Lines)
            {
                var vehicleId = ExtractVehicleIdFromText(line.Content, isExplicitSearch: false);
                if (!string.IsNullOrEmpty(vehicleId))
                {
                    invoiceData.VehicleId = vehicleId;
                    confidenceScores.Add(0.6f); // Medium confidence for general text extraction
                    _logger.LogInformation("Vehicle ID found in document text: {VehicleId}", vehicleId);
                    return;
                }
            }
        }
    }

    private bool ContainsVehicleKeywords(string text)
    {
        var keywords = new[] { "vehicle id", "vehicle number", "unit id", "fleet id", "vin", "license plate", "vehicle:", "unit:", "fleet:" };
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private string? ExtractVehicleIdFromText(string text, bool isExplicitSearch = false)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // For explicit searches (lines that mention vehicle ID), use more targeted patterns
        if (isExplicitSearch)
        {
            var explicitPatterns = new[]
            {
                // Direct vehicle ID patterns after keywords
                @"(?i)vehicle\s*id[:\s]*([A-Z0-9\-]{3,15})",
                @"(?i)vehicle[:\s#\-]*([A-Z0-9\-]{3,15})",
                @"(?i)unit[:\s#\-]*([A-Z0-9\-]{3,15})",
                @"(?i)fleet[:\s#\-]*([A-Z0-9\-]{3,15})",
                
                // VIN patterns (17 characters, alphanumeric)
                @"\b[A-HJ-NPR-Z0-9]{17}\b",
                
                // License plate after keywords
                @"(?i)(?:license|plate|tag)[:\s]*([A-Z0-9\-\s]{3,10})",
                
                // Specific patterns like VEH-013, JEEP-VEH-013
                @"\b([A-Z]+[-]?VEH[-]?\d{3,4})\b",
                @"\b(VEH[-]?\d{3,4})\b",
                
                // After colon patterns (common in forms)
                @":\s*([A-Z0-9\-]{4,15})\b"
            };
            
            foreach (var pattern in explicitPatterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var captured = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    captured = captured.Trim();
                    
                    if (IsValidVehicleId(captured, isStrict: true))
                    {
                        return captured.ToUpperInvariant();
                    }
                }
            }
        }

        // Common vehicle ID patterns in automotive invoices (for general search)
        var patterns = new[]
        {
            // Specific patterns like VEH-013, JEEP-VEH-013 (highest priority)
            @"\b([A-Z]+[-]?VEH[-]?\d{3,4})\b",
            @"\b(VEH[-]?\d{3,4})\b",
            
            // VIN patterns (17 characters, alphanumeric)
            @"\b[A-HJ-NPR-Z0-9]{17}\b",
            
            // Vehicle ID patterns (VEH-, CAR-, AUTO- prefixes)
            @"(?i)(?:vehicle|car|auto|veh)[:\s#\-]*([A-Z0-9\-]{4,15})",
            
            // Stock/Unit number patterns
            @"(?i)(?:stock|unit|fleet)[:\s#\-]*([A-Z0-9\-]{4,15})",
            
            // Generic ID patterns (only if contains "VEH" or similar) - avoid invoice patterns
            @"\b([A-Z]{2,4}[-]VEH[-]?\d{3,6})\b"
        };

        foreach (var pattern in patterns)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var captured = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                captured = captured.Trim();
                
                // Validate the captured text with stricter rules for general search
                if (IsValidVehicleId(captured, isStrict: true)) // Always use strict validation
                {
                    return captured.ToUpperInvariant();
                }
            }
        }

        return null;
    }

    private bool IsValidVehicleId(string candidate, bool isStrict = false)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return false;
        if (candidate.Length < 3 || candidate.Length > 20) return false;
        
        // Remove common noise words
        var lower = candidate.ToLowerInvariant();
        var noiseWords = new[] { "the", "and", "for", "with", "service", "repair", "invoice", "total", "amount", "date", "time", "glass", "auto", "brake", "tire", "oil" };
        
        if (noiseWords.Any(noise => lower.Equals(noise) || (isStrict && lower.Contains(noise)))) return false;
        
        // Should contain at least some alphanumeric content
        if (!System.Text.RegularExpressions.Regex.IsMatch(candidate, @"[A-Za-z0-9]")) return false;
        
        // Avoid pure numbers that might be amounts or dates
        if (System.Text.RegularExpressions.Regex.IsMatch(candidate, @"^\d+\.?\d*$")) return false;
        
        // Reject patterns that look like invoice numbers
        var invoicePatterns = new[] {
            @"^AGE[-]?\d{4}$",      // AGE-2025
            @"^INV[-]?\d{3,6}$",    // INV-1234
            @"^BILL[-]?\d{3,6}$",   // BILL-1234
            @"^[A-Z]{3}[-]?\d{4}[-]?\d{4}$" // AGE-2025-4455 pattern
        };
        
        foreach (var pattern in invoicePatterns)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(candidate, pattern))
            {
                return false; // This looks like an invoice number
            }
        }
        
        // For strict validation, avoid single words that are likely business names
        if (isStrict)
        {
            // Avoid common business words
            var businessWords = new[] { "auto", "glass", "brake", "tire", "service", "center", "shop", "garage", "repair", "parts" };
            if (businessWords.Any(word => lower.Equals(word))) return false;
            
            // Require at least some numbers for vehicle IDs
            if (!System.Text.RegularExpressions.Regex.IsMatch(candidate, @"\d")) return false;
        }
        
        // For vehicle IDs, prefer patterns that contain "VEH" or are clearly vehicle-related
        if (!isStrict)
        {
            // Give preference to patterns containing VEH
            if (candidate.Contains("VEH", StringComparison.OrdinalIgnoreCase)) return true;
            
            // Accept other patterns only if they don't look like invoice numbers
            return true;
        }
        
        return true;
    }

    private string CleanVehicleId(string vehicleId)
    {
        // Remove common prefixes and clean up
        var cleaned = vehicleId.Trim().ToUpperInvariant();
        
        // Remove common prefixes if they exist
        var prefixes = new[] { "VEHICLE:", "CAR:", "AUTO:", "VEH:", "UNIT:", "STOCK:" };
        foreach (var prefix in prefixes)
        {
            if (cleaned.StartsWith(prefix))
            {
                cleaned = cleaned.Substring(prefix.Length).Trim();
                break;
            }
        }
        
        // Clean up spacing and special characters
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", "");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[^\w\-]", "");
        
        return cleaned;
    }

    private void ExtractLineItems(AnalyzedDocument document, InvoiceData invoiceData, List<float> confidenceScores)
    {
        if (document.Fields.TryGetValue("Items", out var itemsField) && itemsField.FieldType == DocumentFieldType.List)
        {
            var lineNumber = 1;
            
            foreach (var itemField in itemsField.Value.AsList())
            {
                if (itemField.FieldType == DocumentFieldType.Dictionary)
                {
                    var item = itemField.Value.AsDictionary();
                    var lineItem = new InvoiceLineData { LineNumber = lineNumber++ };

                    // Debug: Log all available fields for this line item
                    _logger.LogInformation("Items field debugging - Line {LineNumber} has {FieldCount} fields: {Fields}", 
                        lineItem.LineNumber, item.Count, string.Join(", ", item.Keys));
                    
                    // Debug: Log field values for better understanding
                    foreach (var kvp in item)
                    {
                        var fieldValue = kvp.Value?.FieldType switch
                        {
                            DocumentFieldType.String => kvp.Value.Value.AsString(),
                            DocumentFieldType.Double => kvp.Value.Value.AsDouble().ToString(),
                            DocumentFieldType.Int64 => kvp.Value.Value.AsInt64().ToString(),
                            DocumentFieldType.Currency => kvp.Value.Value.AsCurrency().Amount.ToString(),
                            _ => kvp.Value?.ToString() ?? "null"
                        };
                        _logger.LogInformation("Items field debugging - Line {LineNumber}: {FieldName} = '{FieldValue}' (Type: {FieldType})", 
                            lineItem.LineNumber, kvp.Key, fieldValue, kvp.Value?.FieldType);
                    }

                    if (item.TryGetValue("Description", out var descField) && descField.FieldType == DocumentFieldType.String)
                    {
                        lineItem.Description = descField.Value.AsString();
                        confidenceScores.Add(descField.Confidence ?? 0);
                    }

                    if (item.TryGetValue("Amount", out var amountField) && amountField.FieldType == DocumentFieldType.Currency)
                    {
                        lineItem.TotalCost = (decimal)amountField.Value.AsCurrency().Amount;
                        confidenceScores.Add(amountField.Confidence ?? 0);
                    }

                    if (item.TryGetValue("Quantity", out var qtyField) && qtyField.FieldType == DocumentFieldType.Double)
                    {
                        lineItem.Quantity = (decimal)qtyField.Value.AsDouble();
                        confidenceScores.Add(qtyField.Confidence ?? 0);
                    }
                    else
                    {
                        lineItem.Quantity = 1.0m; // Default to 1.0 if not specified (use decimal literal)
                    }

                    if (item.TryGetValue("UnitPrice", out var unitField) && unitField.FieldType == DocumentFieldType.Currency)
                    {
                        lineItem.UnitCost = (decimal)unitField.Value.AsCurrency().Amount;
                        confidenceScores.Add(unitField.Confidence ?? 0);
                    }
                    else if (lineItem.Quantity > 0)
                    {
                        lineItem.UnitCost = lineItem.TotalCost / lineItem.Quantity;
                    }

                    // Classify the line item first
                    lineItem.Category = ClassifyLineItem(lineItem.Description);

                    // Extract part number only for Parts line items
                    if (lineItem.Category == "Parts")
                    {
                        // First, try to get from dedicated PartNumber field
                        if (item.TryGetValue("PartNumber", out var partNumberField) && partNumberField.FieldType == DocumentFieldType.String)
                        {
                            var partNumber = partNumberField.Value.AsString();
                            if (!string.IsNullOrWhiteSpace(partNumber))
                            {
                                lineItem.PartNumber = partNumber.Trim();
                                confidenceScores.Add(partNumberField.Confidence ?? 0);
                                _logger.LogInformation("Items field - Found part number '{PartNumber}' from dedicated PartNumber field for line {LineNumber}", lineItem.PartNumber, lineItem.LineNumber);
                            }
                        }
                        
                        // Fallback: Try to extract from description if no dedicated field found
                        if (string.IsNullOrWhiteSpace(lineItem.PartNumber) && !string.IsNullOrWhiteSpace(lineItem.Description))
                        {
                            var extractedPartNumber = ExtractPartNumberFromDescription(lineItem.Description);
                            if (!string.IsNullOrWhiteSpace(extractedPartNumber))
                            {
                                lineItem.PartNumber = extractedPartNumber;
                                _logger.LogInformation("Items field - Extracted part number '{PartNumber}' from description for line {LineNumber}: '{Description}'", lineItem.PartNumber, lineItem.LineNumber, lineItem.Description);
                            }
                        }
                        
                        // Additional fallback: Try other potential fields
                        if (string.IsNullOrWhiteSpace(lineItem.PartNumber))
                        {
                            // Check for ProductCode, ItemCode, or similar fields
                            var potentialPartNumberFields = new[] { "ProductCode", "ItemCode", "Code", "SKU", "Part" };
                            foreach (var fieldName in potentialPartNumberFields)
                            {
                                if (item.TryGetValue(fieldName, out var codeField) && codeField.FieldType == DocumentFieldType.String)
                                {
                                    var code = codeField.Value.AsString();
                                    if (!string.IsNullOrWhiteSpace(code) && IsLikelyPartNumber(code.Trim()))
                                    {
                                        lineItem.PartNumber = code.Trim();
                                        confidenceScores.Add(codeField.Confidence ?? 0);
                                        _logger.LogInformation("Items field - Found part number '{PartNumber}' from {FieldName} field for line {LineNumber}", lineItem.PartNumber, fieldName, lineItem.LineNumber);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    
                    // Calculate confidence for this line
                    var lineConfidences = new List<float>();
                    if (descField?.Confidence.HasValue == true) lineConfidences.Add(descField.Confidence.Value);
                    if (amountField?.Confidence.HasValue == true) lineConfidences.Add(amountField.Confidence.Value);
                    if (qtyField?.Confidence.HasValue == true) lineConfidences.Add(qtyField.Confidence.Value);
                    if (unitField?.Confidence.HasValue == true) lineConfidences.Add(unitField.Confidence.Value);
                    
                    lineItem.ConfidenceScore = lineConfidences.Any() ? (decimal)(lineConfidences.Average() * 100) : 0;

                    invoiceData.LineItems.Add(lineItem);
                }
            }
        }
    }

    private string? ExtractPartNumberFromDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        // Common part number patterns for automotive parts
        var partNumberPatterns = new[]
        {
            // Honda/Acura format: 15400-RFA-003
            @"\b\d{5}-[A-Z]{3}-\d{3}\b",
            
            // General alphanumeric patterns: ABC123, 12345-ABC, A1B2C3
            @"\b[A-Z0-9]{3,}-[A-Z0-9]{2,}\b",
            @"\b[A-Z0-9]{5,12}\b",
            
            // Pattern with dash: 12345-67890
            @"\b\d{4,6}-\d{3,6}\b",
            
            // Pattern like: P123456, PN123456
            @"\bP[N]?\d{4,8}\b"
        };

        foreach (var pattern in partNumberPatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(description, pattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                var candidate = match.Value.Trim();
                if (IsLikelyPartNumber(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private bool IsLikelyPartNumber(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length < 3)
            return false;

        // Must contain at least one digit or letter
        if (!System.Text.RegularExpressions.Regex.IsMatch(candidate, @"[A-Z0-9]", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return false;

        // Should not be purely numeric unless it's long enough to be a part number
        if (System.Text.RegularExpressions.Regex.IsMatch(candidate, @"^\d+$"))
        {
            return candidate.Length >= 5; // At least 5 digits for numeric part numbers
        }

        // Should not be common words
        var commonWords = new[] { "the", "and", "for", "with", "item", "part", "qty", "each", "service", "oil", "filter" };
        if (commonWords.Any(word => candidate.Equals(word, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Good patterns: contains mix of letters and numbers, or has dashes
        return System.Text.RegularExpressions.Regex.IsMatch(candidate, @"[A-Z].*\d|\d.*[A-Z]|-", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private string ClassifyLineItem(string description)
    {
        var desc = description.ToLowerInvariant();

        // Labor keywords
        if (desc.Contains("labor") || desc.Contains("service") || desc.Contains("diagnostic") || 
            desc.Contains("hour") || desc.Contains("install") || desc.Contains("repair"))
        {
            return "Labor";
        }

        // Tax keywords
        if (desc.Contains("tax") || desc.Contains("sales tax") || desc.Contains("vat"))
        {
            return "Tax";
        }

        // Fee keywords
        if (desc.Contains("fee") || desc.Contains("disposal") || desc.Contains("environmental") || 
            desc.Contains("shop supplies") || desc.Contains("hazmat"))
        {
            return "Fee";
        }

        // Parts keywords (default for most automotive items)
        if (desc.Contains("filter") || desc.Contains("oil") || desc.Contains("brake") || 
            desc.Contains("tire") || desc.Contains("battery") || desc.Contains("part"))
        {
            return "Parts";
        }

        // Default to Parts for unclassified items
        return "Parts";
    }

    private void CalculatePartAndLaborTotals(InvoiceData invoiceData)
    {
        invoiceData.TotalPartsCost = invoiceData.LineItems
            .Where(item => item.Category == "Parts")
            .Sum(item => item.TotalCost);

        invoiceData.TotalLaborCost = invoiceData.LineItems
            .Where(item => item.Category == "Labor")
            .Sum(item => item.TotalCost);

        // If we don't have a total cost from OCR, calculate it
        if (!invoiceData.TotalCost.HasValue)
        {
            invoiceData.TotalCost = invoiceData.LineItems.Sum(item => item.TotalCost);
        }
    }

    #region Enhanced OCR Methods

    private async Task<FormRecognizerResult> AnalyzeWithPrebuiltInvoice(MemoryStream documentStream)
    {
        var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-invoice", documentStream);
        return ProcessAnalysisResult(operation.Value, "prebuilt-invoice");
    }

    private async Task<FormRecognizerResult> AnalyzeWithGeneralDocument(MemoryStream documentStream)
    {
        var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", documentStream);
        return ProcessGeneralDocumentResult(operation.Value);
    }

    private async Task<FormRecognizerResult> AnalyzeWithReadModel(MemoryStream documentStream)
    {
        var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", documentStream);
        return ProcessReadModelResult(operation.Value);
    }

    private bool IsBetterResult(FormRecognizerResult newResult, FormRecognizerResult currentBest)
    {
        // Strongly prefer structured line items from prebuilt invoice model
        // These come from proper invoice field recognition, not table parsing
        var newHasStructuredItems = HasStructuredLineItems(newResult.InvoiceData);
        var currentHasStructuredItems = HasStructuredLineItems(currentBest.InvoiceData);
        
        // If new result has structured items and current doesn't, prefer new
        if (newHasStructuredItems && !currentHasStructuredItems)
            return true;
            
        // If current has structured items and new doesn't, keep current
        if (!newHasStructuredItems && currentHasStructuredItems)
            return false;
            
        // If both have structured items, or both don't, then use other criteria
        
        // Prefer results with line items
        if (newResult.InvoiceData?.LineItems.Count > 0 && currentBest.InvoiceData?.LineItems.Count == 0)
            return true;

        if (newResult.InvoiceData?.LineItems.Count == 0 && currentBest.InvoiceData?.LineItems.Count > 0)
            return false;

        // If both have line items or both don't, prefer higher confidence
        if (newResult.OverallConfidence > currentBest.OverallConfidence + 5) // 5% threshold
            return true;

        // Prefer results with more complete data
        var newCompleteness = CalculateDataCompleteness(newResult.InvoiceData);
        var currentCompleteness = CalculateDataCompleteness(currentBest.InvoiceData);

        return newCompleteness > currentCompleteness;
    }

    private bool HasStructuredLineItems(InvoiceData? data)
    {
        if (data?.LineItems == null || !data.LineItems.Any()) return false;
        
        // Structured line items typically have consistent confidence scores
        // and come from the prebuilt invoice model's "Items" field
        // Table-parsed items often have default confidence scores (like 60 or 70)
        var confidenceScores = data.LineItems
            .Where(item => item.ConfidenceScore > 0)
            .Select(item => item.ConfidenceScore)
            .ToList();
            
        if (!confidenceScores.Any()) return false;
        
        // If most items have confidence scores other than our default table values (60, 70)
        // it's likely from structured extraction
        var nonDefaultScores = confidenceScores.Count(score => score != 60 && score != 70);
        return nonDefaultScores > confidenceScores.Count / 2;
    }

    private int CalculateDataCompleteness(InvoiceData? data)
    {
        if (data == null) return 0;

        int score = 0;
        if (!string.IsNullOrEmpty(data.InvoiceNumber)) score += 20;
        if (data.InvoiceDate.HasValue) score += 15;
        if (data.TotalCost.HasValue && data.TotalCost > 0) score += 25;
        if (!string.IsNullOrEmpty(data.VehicleId)) score += 10;
        if (data.LineItems.Count > 0) score += 20;
        if (data.Odometer.HasValue) score += 10;

        return score;
    }

    private FormRecognizerResult EnhanceExtractionResult(FormRecognizerResult bestResult, 
        List<(string Strategy, FormRecognizerResult Result)> allResults)
    {
        if (bestResult.InvoiceData == null) return bestResult;

        // Merge data from different strategies to fill gaps
        foreach (var (strategy, result) in allResults)
        {
            if (result.Success && result.InvoiceData != null)
            {
                MergeInvoiceData(bestResult.InvoiceData, result.InvoiceData);
            }
        }

        // Apply text enhancement and cleaning
        EnhanceTextExtraction(bestResult.InvoiceData);

        // Recalculate confidence based on enhanced data
        bestResult.OverallConfidence = CalculateEnhancedConfidence(bestResult.InvoiceData, allResults);

        return bestResult;
    }

    private void MergeInvoiceData(InvoiceData primary, InvoiceData secondary)
    {
        // Fill in missing primary data with secondary data
        if (string.IsNullOrEmpty(primary.VehicleId) && !string.IsNullOrEmpty(secondary.VehicleId))
            primary.VehicleId = secondary.VehicleId;

        if (string.IsNullOrEmpty(primary.InvoiceNumber) && !string.IsNullOrEmpty(secondary.InvoiceNumber))
            primary.InvoiceNumber = secondary.InvoiceNumber;

        if (!primary.InvoiceDate.HasValue && secondary.InvoiceDate.HasValue)
            primary.InvoiceDate = secondary.InvoiceDate;

        if (!primary.TotalCost.HasValue && secondary.TotalCost.HasValue)
            primary.TotalCost = secondary.TotalCost;

        if (!primary.Odometer.HasValue && secondary.Odometer.HasValue)
            primary.Odometer = secondary.Odometer;

        // Merge line items (avoid duplicates)
        if (primary.LineItems.Count == 0 && secondary.LineItems.Count > 0)
        {
            primary.LineItems = secondary.LineItems;
        }
    }

    private void EnhanceTextExtraction(InvoiceData data)
    {
        // Clean and normalize extracted text
        if (!string.IsNullOrEmpty(data.VehicleId))
        {
            data.VehicleId = CleanVehicleId(data.VehicleId);
        }

        if (!string.IsNullOrEmpty(data.InvoiceNumber))
        {
            data.InvoiceNumber = CleanInvoiceNumber(data.InvoiceNumber);
        }

        // Enhance line item descriptions
        foreach (var item in data.LineItems)
        {
            item.Description = CleanDescription(item.Description);
            item.Category = ReclassifyLineItem(item.Description, item.Category);
        }
    }

    private string CleanInvoiceNumber(string invoiceNumber)
    {
        // Remove common prefixes and clean up
        var cleaned = invoiceNumber.Trim().ToUpperInvariant();
        if (cleaned.StartsWith("INVOICE:") || cleaned.StartsWith("INV:"))
            return cleaned.Split(':')[1].Trim();
        if (cleaned.StartsWith("#"))
            return cleaned.Substring(1);
        return cleaned;
    }

    private string CleanDescription(string description)
    {
        // Basic text cleaning
        return description?.Trim()
            .Replace("  ", " ")
            .Replace("\n", " ")
            .Replace("\r", "")
            ?? string.Empty;
    }

    private string ReclassifyLineItem(string description, string? currentCategory)
    {
        // Enhanced classification with more keywords
        var desc = description.ToLowerInvariant();

        // Labor keywords (expanded)
        var laborKeywords = new[] { "labor", "service", "diagnostic", "hour", "hrs", "install", "installation", 
            "repair", "maintenance", "inspection", "tune", "flush", "change", "replace" };
        
        if (laborKeywords.Any(keyword => desc.Contains(keyword)))
            return "Labor";

        // Parts keywords (expanded)
        var partsKeywords = new[] { "filter", "oil", "brake", "pad", "rotor", "tire", "battery", "spark", "plug",
            "belt", "fluid", "gasket", "part", "component", "assembly", "kit" };
        
        if (partsKeywords.Any(keyword => desc.Contains(keyword)))
            return "Parts";

        // Tax keywords
        if (desc.Contains("tax") || desc.Contains("vat"))
            return "Tax";

        // Fee keywords (expanded)
        var feeKeywords = new[] { "fee", "disposal", "environmental", "shop supplies", "hazmat", "core", 
            "surcharge", "handling" };
        
        if (feeKeywords.Any(keyword => desc.Contains(keyword)))
            return "Fee";

        // Return current category if no better classification found
        return currentCategory ?? "Parts";
    }

    private decimal CalculateEnhancedConfidence(InvoiceData data, List<(string Strategy, FormRecognizerResult Result)> allResults)
    {
        var baseConfidence = allResults.Where(r => r.Result.Success).Max(r => r.Result.OverallConfidence);
        
        // Boost confidence based on data completeness
        var completeness = CalculateDataCompleteness(data);
        var completenessBonus = (completeness / 100m) * 10; // Max 10% bonus

        // Boost confidence if multiple strategies agree
        var agreementBonus = 0m;
        if (allResults.Count(r => r.Result.Success) > 1)
        {
            agreementBonus = 5m; // 5% bonus for multiple successful extractions
        }

        return Math.Min(100, baseConfidence + completenessBonus + agreementBonus);
    }

    private FormRecognizerResult ProcessGeneralDocumentResult(AnalyzeResult result)
    {
        // Process general document model results (simpler structure)
        var invoiceData = new InvoiceData();
        var confidenceScores = new List<float>();

        // Extract key-value pairs and tables from general document
        ExtractFromKeyValuePairs(result, invoiceData, confidenceScores);
        ExtractFromTables(result, invoiceData, confidenceScores);

        var overallConfidence = confidenceScores.Count > 0 ? confidenceScores.Average() * 100 : 50;

        return new FormRecognizerResult
        {
            Success = true,
            InvoiceData = invoiceData,
            OverallConfidence = (decimal)overallConfidence,
            RawJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
        };
    }

    private FormRecognizerResult ProcessReadModelResult(AnalyzeResult result)
    {
        // Process read model results (text extraction only)
        var invoiceData = new InvoiceData();
        
        // Extract data from raw text using pattern matching
        var allText = string.Join("\n", result.Pages.SelectMany(p => p.Lines.Select(l => l.Content)));
        ExtractFromRawText(allText, invoiceData);

        return new FormRecognizerResult
        {
            Success = true,
            InvoiceData = invoiceData,
            OverallConfidence = 40, // Lower confidence for text-only extraction
            RawJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
        };
    }

    private void ExtractFromKeyValuePairs(AnalyzeResult result, InvoiceData invoiceData, List<float> confidenceScores)
    {
        foreach (var kvp in result.KeyValuePairs)
        {
            var key = kvp.Key?.Content?.ToLowerInvariant() ?? "";
            var value = kvp.Value?.Content ?? "";

            if (key.Contains("vehicle") || key.Contains("vin"))
            {
                invoiceData.VehicleId = value;
                confidenceScores.Add(kvp.Confidence);
            }
            else if (key.Contains("invoice") && key.Contains("number"))
            {
                invoiceData.InvoiceNumber = value;
                confidenceScores.Add(kvp.Confidence);
            }
            else if (key.Contains("date"))
            {
                if (DateTime.TryParse(value, out var date))
                {
                    invoiceData.InvoiceDate = date;
                    confidenceScores.Add(kvp.Confidence);
                }
            }
            else if (key.Contains("total") || key.Contains("amount"))
            {
                var numberStr = System.Text.RegularExpressions.Regex.Match(value, @"[\d,]+\.?\d*").Value.Replace(",", "");
                if (decimal.TryParse(numberStr, out var total))
                {
                    invoiceData.TotalCost = total;
                    confidenceScores.Add(kvp.Confidence);
                }
            }
        }
    }

    private void ExtractFromTables(AnalyzeResult result, InvoiceData invoiceData, List<float> confidenceScores)
    {
        foreach (var table in result.Tables)
        {
            if (table.RowCount <= 1) continue; // Skip tables with only headers

            var headers = new List<string>();
            if (table.RowCount > 0)
            {
                // Get headers from first row
                var firstRowCells = table.Cells.Where(c => c.RowIndex == 0).OrderBy(c => c.ColumnIndex);
                headers = firstRowCells.Select(c => c.Content.ToLowerInvariant()).ToList();
            }

            // Debug: Log detected headers
            _logger.LogInformation("ExtractFromTables - Table headers detected: {Headers}", string.Join(", ", headers.Select((h, i) => $"Col{i}: '{h}'")));

            // Process data rows
            for (int row = 1; row < table.RowCount; row++)
            {
                var rowCells = table.Cells.Where(c => c.RowIndex == row).OrderBy(c => c.ColumnIndex).ToList();
                if (rowCells.Count == 0) continue;

                var lineItem = new InvoiceLineData { LineNumber = row };

                for (int col = 0; col < Math.Min(headers.Count, rowCells.Count); col++)
                {
                    var header = headers[col];
                    var cellValue = rowCells[col].Content;

                    if (header.Contains("description") || header.Contains("item"))
                    {
                        lineItem.Description = cellValue;
                    }
                    else if (header.Contains("part") && (header.Contains("number") || header.Contains("no") || header.Contains("#")))
                    {
                        // Store potential part number - will be filtered by classification later
                        if (!string.IsNullOrWhiteSpace(cellValue))
                        {
                            lineItem.PartNumber = cellValue.Trim();
                            _logger.LogInformation("ExtractFromTables - Found part number '{PartNumber}' in column '{Header}' for line {LineNumber}", cellValue.Trim(), header, row);
                        }
                    }
                    else if (header.Contains("quantity") || header.Contains("qty"))
                    {
                        if (decimal.TryParse(cellValue, out var qty))
                            lineItem.Quantity = qty;
                    }
                    else if (header.Contains("price") || header.Contains("cost"))
                    {
                        var numberStr = System.Text.RegularExpressions.Regex.Match(cellValue, @"[\d,]+\.?\d*").Value.Replace(",", "");
                        if (decimal.TryParse(numberStr, out var price))
                        {
                            if (header.Contains("unit") || header.Contains("each"))
                                lineItem.UnitCost = price;
                            else
                                lineItem.TotalCost = price;
                        }
                    }
                    else if (header.Contains("total") || header.Contains("amount"))
                    {
                        var numberStr = System.Text.RegularExpressions.Regex.Match(cellValue, @"[\d,]+\.?\d*").Value.Replace(",", "");
                        if (decimal.TryParse(numberStr, out var total))
                            lineItem.TotalCost = total;
                    }
                }

                // Set defaults and calculate missing values
                if (lineItem.Quantity == 0) lineItem.Quantity = 1.0m;
                if (lineItem.UnitCost == 0 && lineItem.TotalCost > 0)
                    lineItem.UnitCost = lineItem.TotalCost / lineItem.Quantity;
                if (lineItem.TotalCost == 0 && lineItem.UnitCost > 0)
                    lineItem.TotalCost = lineItem.UnitCost * lineItem.Quantity;

                // Classify the line item
                lineItem.Category = ClassifyLineItem(lineItem.Description);

                // Only keep part numbers for Parts line items - clear for others
                if (lineItem.Category != "Parts")
                {
                    lineItem.PartNumber = null;
                }

                lineItem.ConfidenceScore = 60; // Medium confidence for table extraction

                if (!string.IsNullOrEmpty(lineItem.Description))
                {
                    invoiceData.LineItems.Add(lineItem);
                    confidenceScores.Add(0.6f);
                }
            }
        }
    }

    private void ExtractFromRawText(string text, InvoiceData invoiceData)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Use regex patterns to extract data
        foreach (var line in lines)
        {
            // Vehicle ID patterns
            var vehicleMatch = System.Text.RegularExpressions.Regex.Match(line, @"(?i)(?:vehicle|vin|car)\s*:?\s*([A-Z0-9-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (vehicleMatch.Success && string.IsNullOrEmpty(invoiceData.VehicleId))
            {
                invoiceData.VehicleId = vehicleMatch.Groups[1].Value;
            }

            // Invoice number patterns
            var invoiceMatch = System.Text.RegularExpressions.Regex.Match(line, @"(?i)(?:invoice|inv)\s*#?\s*:?\s*([A-Z0-9-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (invoiceMatch.Success && string.IsNullOrEmpty(invoiceData.InvoiceNumber))
            {
                invoiceData.InvoiceNumber = invoiceMatch.Groups[1].Value;
            }

            // Date patterns
            var dateMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}-\d{2}-\d{2})");
            if (dateMatch.Success && !invoiceData.InvoiceDate.HasValue)
            {
                if (DateTime.TryParse(dateMatch.Value, out var date))
                {
                    invoiceData.InvoiceDate = date;
                }
            }

            // Total amount patterns
            var totalMatch = System.Text.RegularExpressions.Regex.Match(line, @"(?i)total[:\s]*\$?([\d,]+\.?\d*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (totalMatch.Success && !invoiceData.TotalCost.HasValue)
            {
                var amountStr = totalMatch.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(amountStr, out var total))
                {
                    invoiceData.TotalCost = total;
                }
            }
        }
    }

    private FormRecognizerResult ProcessAnalysisResult(AnalyzeResult result, string modelType = "default")
    {
        var invoiceData = new InvoiceData();
        var confidenceScores = new List<float>();

        // Extract invoice-level information
        foreach (var document in result.Documents)
        {
            foreach (var field in document.Fields)
            {
                ProcessDocumentField(field.Key, field.Value, invoiceData, confidenceScores);
            }
        }

        // Extract line items from tables
        foreach (var table in result.Tables)
        {
            ProcessTableForLineItems(table, invoiceData, confidenceScores);
        }

        // For prebuilt invoice model, supplement missing part numbers from table data
        if (modelType == "prebuilt-invoice")
        {
            SupplementPartNumbersFromTables(result, invoiceData, confidenceScores);
        }

        // Calculate derived fields
        CalculatePartAndLaborTotals(invoiceData);

        var overallConfidence = confidenceScores.Count > 0 
            ? (decimal)(confidenceScores.Average() * 100)
            : 75m; // Default confidence for prebuilt models

        return new FormRecognizerResult
        {
            Success = true,
            InvoiceData = invoiceData,
            OverallConfidence = overallConfidence,
            RawJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
        };
    }

    private void ProcessDocumentField(string fieldName, DocumentField field, InvoiceData invoiceData, List<float> confidenceScores)
    {
        if (field?.Value == null) return;

        var confidence = field.Confidence ?? 0.5f;
        confidenceScores.Add(confidence);

        switch (fieldName.ToLowerInvariant())
        {
            case "invoiceid":
            case "invoicenumber":
                invoiceData.InvoiceNumber = field.Value.AsString();
                break;

            case "invoicedate":
            case "date":
                try
                {
                    invoiceData.InvoiceDate = field.Value.AsDate().DateTime;
                }
                catch
                {
                    if (DateTime.TryParse(field.Value.AsString(), out var parsedDate))
                    {
                        invoiceData.InvoiceDate = parsedDate;
                    }
                }
                break;

            case "invoicetotal":
            case "totalamount":
            case "amountdue":
                try
                {
                    invoiceData.TotalCost = (decimal)field.Value.AsCurrency().Amount;
                }
                catch
                {
                    if (decimal.TryParse(field.Value.AsString(), out var parsedDecimal))
                    {
                        invoiceData.TotalCost = parsedDecimal;
                    }
                }
                break;

            case "customername":
            case "vendorname":
                // These could be used for additional invoice metadata in future
                break;

            case "items":
                // Process line items if they come as structured data
                if (field.Value is IList<object> items)
                {
                    ProcessStructuredLineItems(items, invoiceData, confidenceScores);
                }
                break;
        }
    }

    private void ProcessTableForLineItems(DocumentTable table, InvoiceData invoiceData, List<float> confidenceScores)
    {
        if (table.RowCount <= 1) return; // Need at least header + data

        // Get column headers from first row
        var headers = new Dictionary<int, string>();
        var headerCells = table.Cells.Where(c => c.RowIndex == 0).ToList();
        foreach (var cell in headerCells)
        {
            headers[cell.ColumnIndex] = cell.Content?.ToLowerInvariant() ?? "";
        }

        // Debug: Log detected headers
        _logger.LogInformation("Table headers detected: {Headers}", string.Join(", ", headers.Values.Select((h, i) => $"Col{i}: '{h}'")));

        // Process data rows
        for (int rowIndex = 1; rowIndex < table.RowCount; rowIndex++)
        {
            var rowCells = table.Cells.Where(c => c.RowIndex == rowIndex).ToList();
            if (!rowCells.Any()) continue;

            var lineItem = new InvoiceLineData { LineNumber = rowIndex };
            bool hasData = false;

            foreach (var cell in rowCells)
            {
                if (!headers.ContainsKey(cell.ColumnIndex)) continue;

                var header = headers[cell.ColumnIndex];
                var content = cell.Content ?? "";
                
                if (string.IsNullOrWhiteSpace(content)) continue;

                if (header.Contains("description") || header.Contains("item") || header.Contains("service"))
                {
                    lineItem.Description = content;
                    hasData = true;
                }
                else if (header.Contains("part") && (header.Contains("number") || header.Contains("no") || header.Contains("#")))
                {
                    // Store potential part number - will be filtered by classification later
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        lineItem.PartNumber = content.Trim();
                        _logger.LogInformation("Found part number '{PartNumber}' in column '{Header}' for line {LineNumber}", content.Trim(), header, rowIndex);
                    }
                    hasData = true;
                }
                else if (header.Contains("quantity") || header.Contains("qty"))
                {
                    if (decimal.TryParse(content, out var qty))
                        lineItem.Quantity = qty;
                }
                else if (header.Contains("unit") && (header.Contains("price") || header.Contains("cost")))
                {
                    var numStr = ExtractNumericValue(content);
                    if (decimal.TryParse(numStr, out var unitCost))
                        lineItem.UnitCost = unitCost;
                }
                else if (header.Contains("total") || header.Contains("amount"))
                {
                    var numStr = ExtractNumericValue(content);
                    if (decimal.TryParse(numStr, out var total))
                        lineItem.TotalCost = total;
                }
                else if (header.Contains("price") || header.Contains("cost"))
                {
                    var numStr = ExtractNumericValue(content);
                    if (decimal.TryParse(numStr, out var cost))
                    {
                        if (lineItem.UnitCost == 0)
                            lineItem.UnitCost = cost;
                        if (lineItem.TotalCost == 0)
                            lineItem.TotalCost = cost;
                    }
                }

                // Note: DocumentTableCell doesn't have confidence in the current SDK version
                // We'll use a default confidence for table data
                confidenceScores.Add(0.7f);
            }

            if (hasData && !string.IsNullOrEmpty(lineItem.Description))
            {
                // Set defaults and calculate missing values
                if (lineItem.Quantity == 0) lineItem.Quantity = 1.0m;
                if (lineItem.UnitCost == 0 && lineItem.TotalCost > 0)
                    lineItem.UnitCost = lineItem.TotalCost / lineItem.Quantity;
                if (lineItem.TotalCost == 0 && lineItem.UnitCost > 0)
                    lineItem.TotalCost = lineItem.UnitCost * lineItem.Quantity;

                // Classify the line item
                lineItem.Category = ClassifyLineItem(lineItem.Description);

                // Only keep part numbers for Parts line items - clear for others
                if (lineItem.Category != "Parts")
                {
                    lineItem.PartNumber = null;
                }

                lineItem.ConfidenceScore = 70; // Good confidence for table extraction

                invoiceData.LineItems.Add(lineItem);
            }
        }
    }

    private void ProcessStructuredLineItems(IList<object> items, InvoiceData invoiceData, List<float> confidenceScores)
    {
        // Process structured line items from prebuilt invoice model
        foreach (var item in items.Take(50)) // Limit to prevent excessive processing
        {
            // This would need to be adapted based on the actual structure returned by Form Recognizer
            // For now, we'll skip this advanced processing
        }
    }

    private string ExtractNumericValue(string text)
    {
        if (string.IsNullOrEmpty(text)) return "0";
        
        // Remove currency symbols, commas, and extract numbers
        var cleaned = System.Text.RegularExpressions.Regex.Match(text, @"[\d,]+\.?\d*").Value;
        return string.IsNullOrEmpty(cleaned) ? "0" : cleaned.Replace(",", "");
    }

    private bool IsSummaryRow(string description)
    {
        if (string.IsNullOrEmpty(description)) return false;

        var desc = description.Trim().ToLowerInvariant();
        
        // Remove common punctuation and normalize
        desc = System.Text.RegularExpressions.Regex.Replace(desc, @"[:\(\)\[\]\-_\.]", " ");
        desc = System.Text.RegularExpressions.Regex.Replace(desc, @"\s+", " ").Trim();
        
        // If it looks like a legitimate part or service description, don't filter it
        if (IsLikelyValidLineItem(desc))
            return false;
        
        // Common summary/total row patterns that should not be treated as line items
        var summaryKeywords = new[]
        {
            "subtotal",
            "sub total",
            "total",
            "grand total",
            "invoice total",
            "net total",
            "gross total",
            "amount due",
            "balance due",
            "amount owing",
            "total due",
            "total amount",
            "final total",
            "tax",
            "taxes",
            "sales tax",
            "tax total",
            "total tax",
            "vat",
            "gst",
            "hst",
            "pst",
            "qst",
            "discount",
            "discounts",
            "total discount",
            "shipping",
            "shipping cost",
            "shipping total",
            "freight",
            "delivery",
            "delivery charge",
            "handling",
            "handling fee",
            "processing fee",
            "convenience fee",
            "service fee",
            "administrative fee",
            "admin fee",
            "surcharge",
            "miscellaneous",
            "misc",
            "other charges",
            "additional charges",
            "extra charges"
        };

        // Check exact matches first
        if (summaryKeywords.Any(keyword => desc.Equals(keyword)))
            return true;

        // Check if description contains summary keywords
        if (summaryKeywords.Any(keyword => desc.Contains(keyword)))
            return true;

        // Check for patterns like "8.75% tax", "tax (8.75%)", etc.
        if (System.Text.RegularExpressions.Regex.IsMatch(desc, @"^\d+\.?\d*\s*%?\s*(tax|vat|gst|hst)"))
            return true;

        if (System.Text.RegularExpressions.Regex.IsMatch(desc, @"(tax|vat|gst|hst)\s*\(\d+\.?\d*\s*%\)"))
            return true;

        // Check for currency-only patterns that might be totals/subtotals without descriptive text
        // But be careful not to filter legitimate part numbers or quantities
        if (System.Text.RegularExpressions.Regex.IsMatch(desc, @"^[\$€£¥]\s*\d+[,.]?\d*\s*$"))
            return true;

        // Check for standalone decimal numbers that are likely monetary totals (avoid filtering integers that might be part numbers)
        if (System.Text.RegularExpressions.Regex.IsMatch(desc, @"^\d+\.\d{2}\s*$"))
            return true;

        return false;
    }

    private bool IsLikelyValidLineItem(string description)
    {
        if (string.IsNullOrEmpty(description)) return false;

        var desc = description.ToLowerInvariant();

        // Common automotive parts and services keywords
        var validItemKeywords = new[]
        {
            "brake", "pad", "rotor", "disc", "caliper",
            "oil", "filter", "air filter", "fuel filter", "cabin filter",
            "spark plug", "ignition", "coil", "wire",
            "tire", "wheel", "rim", "bearing",
            "belt", "hose", "gasket", "seal",
            "battery", "alternator", "starter", "fuse",
            "fluid", "coolant", "transmission", "differential",
            "suspension", "shock", "strut", "spring",
            "exhaust", "muffler", "catalytic", "pipe",
            "wiper", "blade", "bulb", "headlight", "taillight",
            "sensor", "switch", "relay", "module",
            "labor", "service", "installation", "repair", "maintenance",
            "inspection", "diagnostic", "alignment", "balancing",
            "flush", "change", "replacement", "adjustment"
        };

        return validItemKeywords.Any(keyword => desc.Contains(keyword));
    }

    /// <summary>
    /// Supplement missing part numbers from table data when prebuilt invoice model Items field doesn't contain them
    /// </summary>
    private void SupplementPartNumbersFromTables(AnalyzeResult result, InvoiceData invoiceData, List<float> confidenceScores)
    {
        _logger.LogInformation("SupplementPartNumbersFromTables - Checking {TableCount} tables for part numbers", result.Tables.Count);

        foreach (var table in result.Tables)
        {
            if (table.RowCount <= 1) continue; // Need at least header + data

            // Get column headers from first row
            var headers = new Dictionary<int, string>();
            var headerCells = table.Cells.Where(c => c.RowIndex == 0).ToList();
            foreach (var cell in headerCells)
            {
                headers[cell.ColumnIndex] = cell.Content?.ToLowerInvariant() ?? "";
            }

            _logger.LogInformation("SupplementPartNumbersFromTables - Table headers detected: {Headers}", 
                string.Join(", ", headers.Values.Select((h, i) => $"Col{i}: '{h}'")));

            // Find part number column
            int partNumberColumnIndex = -1;
            foreach (var header in headers)
            {
                if (header.Value.Contains("part") && (header.Value.Contains("number") || header.Value.Contains("no") || header.Value.Contains("#")))
                {
                    partNumberColumnIndex = header.Key;
                    _logger.LogInformation("SupplementPartNumbersFromTables - Found part number column at index {ColumnIndex}: '{Header}'", 
                        partNumberColumnIndex, header.Value);
                    break;
                }
            }

            if (partNumberColumnIndex == -1)
            {
                _logger.LogInformation("SupplementPartNumbersFromTables - No part number column found in this table");
                continue;
            }

            // Find description column for matching
            int descriptionColumnIndex = -1;
            foreach (var header in headers)
            {
                if (header.Value.Contains("description") || header.Value.Contains("item"))
                {
                    descriptionColumnIndex = header.Key;
                    break;
                }
            }

            // Process data rows and match with existing line items
            for (int rowIndex = 1; rowIndex < table.RowCount; rowIndex++)
            {
                var partNumberCell = table.Cells.FirstOrDefault(c => c.RowIndex == rowIndex && c.ColumnIndex == partNumberColumnIndex);
                var descriptionCell = table.Cells.FirstOrDefault(c => c.RowIndex == rowIndex && c.ColumnIndex == descriptionColumnIndex);

                if (partNumberCell == null || string.IsNullOrWhiteSpace(partNumberCell.Content)) continue;

                var partNumber = partNumberCell.Content.Trim();
                var tableDescription = descriptionCell?.Content?.Trim() ?? "";

                _logger.LogInformation("SupplementPartNumbersFromTables - Row {RowIndex}: Part Number = '{PartNumber}', Description = '{Description}'", 
                    rowIndex, partNumber, tableDescription);

                // Find matching line item in invoiceData by description or line number
                InvoiceLineData? matchingItem = null;
                
                // Try to match by description first
                if (!string.IsNullOrEmpty(tableDescription))
                {
                    matchingItem = invoiceData.LineItems.FirstOrDefault(item => 
                        !string.IsNullOrEmpty(item.Description) && 
                        item.Description.Contains(tableDescription, StringComparison.OrdinalIgnoreCase));
                }

                // If no match by description, try by line number (accounting for header row)
                if (matchingItem == null && rowIndex <= invoiceData.LineItems.Count)
                {
                    matchingItem = invoiceData.LineItems.ElementAtOrDefault(rowIndex - 1);
                }

                // Update part number if we found a match and it doesn't already have a part number
                if (matchingItem != null && string.IsNullOrEmpty(matchingItem.PartNumber))
                {
                    matchingItem.PartNumber = partNumber;
                    _logger.LogInformation("SupplementPartNumbersFromTables - Added part number '{PartNumber}' to line item '{Description}'", 
                        partNumber, matchingItem.Description);
                }
                else if (matchingItem != null)
                {
                    _logger.LogInformation("SupplementPartNumbersFromTables - Line item '{Description}' already has part number '{ExistingPartNumber}'", 
                        matchingItem.Description, matchingItem.PartNumber);
                }
                else
                {
                    _logger.LogInformation("SupplementPartNumbersFromTables - Could not find matching line item for part number '{PartNumber}'", partNumber);
                }
            }
        }
    }

    #endregion
}
