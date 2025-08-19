using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleMaintenanceInvoiceSystem.Models;

/// <summary>
/// Represents an invoice header with summary information
/// </summary>
public class InvoiceHeader
{
    [Key]
    public int InvoiceID { get; set; }

    [Required]
    [StringLength(50)]
    public string VehicleID { get; set; } = string.Empty;

    public int? Odometer { get; set; }

    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    public DateTime InvoiceDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPartsCost { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLaborCost { get; set; }

    [Required]
    [StringLength(255)]
    public string BlobFileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Raw JSON representation of extracted data from Form Recognizer
    /// </summary>
    public string? ExtractedData { get; set; }

    /// <summary>
    /// Overall confidence score (0-100) for OCR extraction
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? ConfidenceScore { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Indicates whether this invoice has been approved for payment
    /// </summary>
    [Required]
    public bool Approved { get; set; } = false;

    /// <summary>
    /// Timestamp when the invoice was approved (null if not approved)
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// User identifier who approved the invoice (null if not approved)
    /// </summary>
    [StringLength(100)]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Original extracted vehicle field label before normalization
    /// </summary>
    [StringLength(100)]
    public string? OriginalVehicleLabel { get; set; }

    /// <summary>
    /// Original extracted odometer field label before normalization
    /// </summary>
    [StringLength(100)]
    public string? OriginalOdometerLabel { get; set; }

    /// <summary>
    /// Original extracted invoice field label before normalization
    /// </summary>
    [StringLength(100)]
    public string? OriginalInvoiceLabel { get; set; }

    /// <summary>
    /// Version of field normalization rules applied
    /// </summary>
    [StringLength(20)]
    public string? NormalizationVersion { get; set; }

    // Navigation properties
    public virtual ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
    public virtual ICollection<FieldNormalizationFeedback> FieldNormalizationFeedbacks { get; set; } = new List<FieldNormalizationFeedback>();
}
