using System.Text.Json;

namespace VehicleMaintenanceInvoiceSystem.Services
{
    public interface IGitHubModelsService
    {
        Task<string> ProcessInvoiceTextAsync(string extractedText, string prompt);
        Task<InvoiceEnhancementResult> EnhanceInvoiceDataAsync(string formRecognizerData);
        Task<ComprehensiveInvoiceProcessingResult> ProcessInvoiceComprehensivelyAsync(string formRecognizerData);
        Task<bool> TestConnectionAsync();
    }

    public class InvoiceEnhancementResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public Dictionary<string, object> EnhancedData { get; set; } = new();
        public double ConfidenceScore { get; set; }
        public List<string> Improvements { get; set; } = new();
    }

    public class ComprehensiveInvoiceProcessingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string ProcessingMethod { get; set; } = "GPT4o-Enhanced";
        public bool RateLimitEncountered { get; set; }
        
        // Header Information
        public string? VehicleId { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public int? Odometer { get; set; }
        public decimal? TotalCost { get; set; }
        public string? Description { get; set; }
        
        // Line Items
        public List<ProcessedLineItem> LineItems { get; set; } = new();
        
        // Confidence and Processing Notes
        public decimal OverallConfidence { get; set; }
        public List<string> ProcessingNotes { get; set; } = new();
    }

    public class ProcessedLineItem
    {
        public int LineNumber { get; set; }
        public string Description { get; set; } = "";
        public string Classification { get; set; } = "";
        public decimal UnitCost { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalCost { get; set; }
        public string? PartNumber { get; set; }
        public decimal Confidence { get; set; }
    }
}
