namespace VehicleMaintenanceInvoiceSystem.Services;

/// <summary>
/// Interface for fallback processing when AI services are unavailable
/// </summary>
public interface IInvoiceFallbackService
{
    /// <summary>
    /// Process invoice using rule-based fallback methods
    /// </summary>
    Task<ComprehensiveInvoiceProcessingResult> ProcessInvoiceAsync(string rawOcrData, InvoiceData invoiceData);

    /// <summary>
    /// Check if fallback service is available
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Initialize the fallback service
    /// </summary>
    Task InitializeAsync();
}
