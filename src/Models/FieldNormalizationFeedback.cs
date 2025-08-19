using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleMaintenanceInvoiceSystem.Models;

/// <summary>
/// Represents user feedback for field label normalization corrections
/// Used to improve field mapping accuracy over time
/// </summary>
public class FieldNormalizationFeedback
{
    [Key]
    public int NormalizationFeedbackID { get; set; }

    [Required]
    public int InvoiceID { get; set; }

    /// <summary>
    /// The original field label extracted from the invoice
    /// </summary>
    [Required]
    [StringLength(100)]
    public string OriginalLabel { get; set; } = string.Empty;

    /// <summary>
    /// The system's current normalization of the field
    /// </summary>
    [Required]
    [StringLength(100)]
    public string CurrentNormalization { get; set; } = string.Empty;

    /// <summary>
    /// The user's expected/correct normalization
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ExpectedNormalization { get; set; } = string.Empty;

    /// <summary>
    /// Type of field (VehicleID, InvoiceNumber, Odometer, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// Optional user comment explaining the correction
    /// </summary>
    [StringLength(500)]
    public string? UserComment { get; set; }

    /// <summary>
    /// Identifier of the user who provided the feedback
    /// </summary>
    [Required]
    [StringLength(100)]
    public string UserID { get; set; } = string.Empty;

    /// <summary>
    /// When the feedback was submitted
    /// </summary>
    [Required]
    public DateTime FeedbackDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version of the normalization rules that made the original mapping
    /// </summary>
    [StringLength(20)]
    public string? NormalizationVersion { get; set; }

    /// <summary>
    /// Whether this feedback has been applied to normalization rules
    /// </summary>
    [Required]
    public bool Applied { get; set; } = false;

    // Navigation property
    [ForeignKey("InvoiceID")]
    public virtual InvoiceHeader InvoiceHeader { get; set; } = null!;
}
