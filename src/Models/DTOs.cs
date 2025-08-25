namespace VehicleMaintenanceInvoiceSystem.Models;

/// <summary>
/// Configuration settings for Azure Blob Storage
/// </summary>
public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public int MaxFileSizeMB { get; set; } = 10;
}

/// <summary>
/// Configuration settings for Azure Form Recognizer
/// </summary>
public class FormRecognizerOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "prebuilt-invoice";
}

/// <summary>
/// Response model for file upload operations
/// </summary>
public class FileUploadResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? BlobUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
}

/// <summary>
/// Information about a file stored in blob storage
/// </summary>
public class BlobFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response model for invoice processing operations
/// </summary>
public class InvoiceProcessingResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? InvoiceId { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// DTO for paginated results
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}

/// <summary>
/// DTO for invoice summary (header only)
/// </summary>
public class InvoiceSummaryDto
{
    public int InvoiceID { get; set; }
    public string VehicleID { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalPartsCost { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Approved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public int LineItemCount { get; set; }
    public List<InvoiceLineDto> LineItems { get; set; } = new(); // Added this line
}

/// <summary>
/// DTO for complete invoice details with line items
/// </summary>
public class InvoiceDetailsDto
{
    public int InvoiceID { get; set; }
    public string VehicleID { get; set; } = string.Empty;
    public int? Odometer { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalPartsCost { get; set; }
    public decimal TotalLaborCost { get; set; }
    public string BlobFileUrl { get; set; } = string.Empty;
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Approved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public List<InvoiceLineDto> LineItems { get; set; } = new();
}

/// <summary>
/// DTO for invoice line items
/// </summary>
public class InvoiceLineDto
{
    public int LineID { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalLineCost { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }
    public decimal? ConfidenceScore { get; set; }
}

/// <summary>
/// Request model for approving an invoice
/// </summary>
public class ApproveInvoiceRequest
{
    public string ApprovedBy { get; set; } = string.Empty;
}

/// <summary>
/// Response model for approval/rejection operations
/// </summary>
public class InvoiceActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string Action { get; set; } = string.Empty; // "approved" or "rejected"
    public DateTime? ActionTimestamp { get; set; }
    public string? ActionBy { get; set; }
}
