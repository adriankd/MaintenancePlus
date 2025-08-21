using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleMaintenanceInvoiceSystem.Models;

/// <summary>
/// Tracks classification accuracy metrics over time for monitoring and improvement
/// </summary>
public class ClassificationAccuracyLog
{
    [Key]
    public int LogID { get; set; }

    /// <summary>
    /// Date period this accuracy measurement covers
    /// </summary>
    [Required]
    public DateTime DatePeriod { get; set; }

    /// <summary>
    /// Type of classification (Part, Labor, Overall, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ClassificationType { get; set; } = string.Empty;

    /// <summary>
    /// Version of the classification model
    /// </summary>
    [Required]
    [StringLength(20)]
    public string ModelVersion { get; set; } = string.Empty;

    /// <summary>
    /// Total number of classifications made in this period
    /// </summary>
    [Required]
    public int TotalClassifications { get; set; }

    /// <summary>
    /// Number of correct classifications in this period
    /// </summary>
    [Required]
    public int CorrectClassifications { get; set; }

    /// <summary>
    /// Calculated accuracy percentage (0-100)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal AccuracyPercentage { get; set; }

    /// <summary>
    /// Average confidence score for classifications in this period
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? AverageConfidence { get; set; }

    /// <summary>
    /// Number of user feedback corrections received in this period
    /// </summary>
    [Required]
    public int FeedbackCount { get; set; } = 0;

    /// <summary>
    /// When this log entry was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
