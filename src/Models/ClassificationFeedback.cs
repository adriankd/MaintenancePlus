using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleMaintenanceInvoiceSystem.Models;

/// <summary>
/// Represents user feedback for line item classification corrections
/// Used for continuous learning and model improvement
/// </summary>
public class ClassificationFeedback
{
    [Key]
    public int FeedbackID { get; set; }

    [Required]
    public int LineID { get; set; }

    [Required]
    public int InvoiceID { get; set; }

    /// <summary>
    /// The system's original classification (Part, Labor, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OriginalClassification { get; set; } = string.Empty;

    /// <summary>
    /// The user's corrected classification
    /// </summary>
    [Required]
    [StringLength(50)]
    public string CorrectedClassification { get; set; } = string.Empty;

    /// <summary>
    /// The system's confidence in the original classification (0-100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? OriginalConfidence { get; set; }

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
    /// Version of the classification model that made the original classification
    /// </summary>
    [StringLength(20)]
    public string? ModelVersion { get; set; }

    /// <summary>
    /// Whether this feedback has been applied to model training
    /// </summary>
    [Required]
    public bool Applied { get; set; } = false;

    // Navigation properties
    [ForeignKey("LineID")]
    public virtual InvoiceLine InvoiceLine { get; set; } = null!;

    [ForeignKey("InvoiceID")]
    public virtual InvoiceHeader InvoiceHeader { get; set; } = null!;
}
