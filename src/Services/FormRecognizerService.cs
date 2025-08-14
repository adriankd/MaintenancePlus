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

                // Try to extract odometer from custom fields or text analysis
                ExtractOdometerReading(result, invoiceData, confidenceScores);

                // Extract line items
                ExtractLineItems(document, invoiceData, confidenceScores);

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
                    // Try to extract number from the line
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

                    // Classify the line item
                    lineItem.Category = ClassifyLineItem(lineItem.Description);
                    
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
}
