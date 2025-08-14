using Microsoft.EntityFrameworkCore;
using VehicleMaintenanceInvoiceSystem.Models;

namespace VehicleMaintenanceInvoiceSystem.Data;

/// <summary>
/// Entity Framework DbContext for Vehicle Maintenance Invoice System
/// </summary>
public class InvoiceDbContext : DbContext
{
    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options)
    {
    }

    public DbSet<InvoiceHeader> InvoiceHeaders { get; set; }
    public DbSet<InvoiceLine> InvoiceLines { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure InvoiceHeader
        modelBuilder.Entity<InvoiceHeader>(entity =>
        {
            entity.ToTable("InvoiceHeader");
            entity.HasKey(e => e.InvoiceID);
            entity.HasIndex(e => e.VehicleID).HasDatabaseName("IX_InvoiceHeader_VehicleID");
            entity.HasIndex(e => e.InvoiceDate).HasDatabaseName("IX_InvoiceHeader_InvoiceDate");
            entity.HasIndex(e => e.InvoiceNumber).HasDatabaseName("IX_InvoiceHeader_InvoiceNumber").IsUnique();
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_InvoiceHeader_CreatedAt");

            // Explicit decimal precision for cost fields
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPartsCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalLaborCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ConfidenceScore).HasColumnType("decimal(5,2)");

            // Constraints
            entity.HasCheckConstraint("CK_InvoiceHeader_TotalCost", "TotalCost >= 0");
            entity.HasCheckConstraint("CK_InvoiceHeader_TotalPartsCost", "TotalPartsCost >= 0");
            entity.HasCheckConstraint("CK_InvoiceHeader_TotalLaborCost", "TotalLaborCost >= 0");
            entity.HasCheckConstraint("CK_InvoiceHeader_Odometer", "Odometer IS NULL OR Odometer >= 0");
            entity.HasCheckConstraint("CK_InvoiceHeader_ConfidenceScore", "ConfidenceScore IS NULL OR (ConfidenceScore >= 0 AND ConfidenceScore <= 100)");

            // Default values
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        // Configure InvoiceLine
        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.ToTable("InvoiceLines");
            entity.HasKey(e => e.LineID);
            entity.HasIndex(e => e.InvoiceID).HasDatabaseName("IX_InvoiceLines_InvoiceID");
            entity.HasIndex(e => new { e.InvoiceID, e.LineNumber }).HasDatabaseName("IX_InvoiceLines_InvoiceID_LineNumber");
            entity.HasIndex(e => e.Category).HasDatabaseName("IX_InvoiceLines_Category");
            entity.HasIndex(e => e.PartNumber).HasDatabaseName("IX_InvoiceLines_PartNumber");

            // Explicit decimal precision for cost and quantity fields
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TotalLineCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ConfidenceScore).HasColumnType("decimal(5,2)");

            // Constraints
            entity.HasCheckConstraint("CK_InvoiceLines_UnitCost", "UnitCost >= 0");
            entity.HasCheckConstraint("CK_InvoiceLines_Quantity", "Quantity > 0");
            entity.HasCheckConstraint("CK_InvoiceLines_TotalLineCost", "TotalLineCost >= 0");
            entity.HasCheckConstraint("CK_InvoiceLines_LineNumber", "LineNumber > 0");
            entity.HasCheckConstraint("CK_InvoiceLines_ConfidenceScore", "ConfidenceScore IS NULL OR (ConfidenceScore >= 0 AND ConfidenceScore <= 100)");

            // Foreign key relationship
            entity.HasOne(e => e.InvoiceHeader)
                  .WithMany(e => e.InvoiceLines)
                  .HasForeignKey(e => e.InvoiceID)
                  .OnDelete(DeleteBehavior.Cascade);

            // Default values
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        });
    }
}
