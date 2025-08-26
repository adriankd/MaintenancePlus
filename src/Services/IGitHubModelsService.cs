using System.Text.Json;

namespace VehicleMaintenanceInvoiceSystem.Services
{
    public interface IGitHubModelsService
    {
        Task<string> ProcessInvoiceTextAsync(string extractedText, string prompt);
        Task<InvoiceEnhancementResult> EnhanceInvoiceDataAsync(string formRecognizerData);
    }

    public class InvoiceEnhancementResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public Dictionary<string, object> EnhancedData { get; set; } = new();
        public double ConfidenceScore { get; set; }
        public List<string> Improvements { get; set; } = new();
    }
}
