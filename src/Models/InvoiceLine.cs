using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleMaintenanceInvoiceSystem.Models;

/// <summary>
/// Represents an individual line item within an invoice
/// </summary>
public class InvoiceLine
{
    [Key]
    public int LineID { get; set; }

    [Required]
    public int InvoiceID { get; set; }

    [Required]
    public int LineNumber { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLineCost { get; set; }

    /// <summary>
    /// Optional part number if extracted separately from description
    /// </summary>
    [StringLength(100)]
    public string? PartNumber { get; set; }

    /// <summary>
    /// Category classification: Parts, Labor, Tax, Fee, Service, etc.
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// ML-classified category: Part, Labor (result of intelligent classification)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ClassifiedCategory { get; set; } = "Unclassified";

    /// <summary>
    /// Confidence score for ML classification (0-100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? ClassificationConfidence { get; set; }

    /// <summary>
    /// Method used for classification (Rule-based, ML.NET, Azure-AI, Manual)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ClassificationMethod { get; set; } = "Rule-based";

    /// <summary>
    /// Version of classification model/rules used
    /// </summary>
    [StringLength(20)]
    public string? ClassificationVersion { get; set; }

    /// <summary>
    /// Original extracted category before intelligent classification
    /// </summary>
    [StringLength(100)]
    public string? OriginalCategory { get; set; }

    /// <summary>
    /// Line-level confidence score (0-100) for OCR extraction
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? ExtractionConfidence { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey("InvoiceID")]
    public virtual InvoiceHeader InvoiceHeader { get; set; } = null!;
    
    public virtual ICollection<ClassificationFeedback> ClassificationFeedbacks { get; set; } = new List<ClassificationFeedback>();
}
