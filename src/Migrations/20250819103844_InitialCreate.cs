using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleMaintenanceInvoiceSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassificationAccuracyLog",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatePeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClassificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalClassifications = table.Column<int>(type: "int", nullable: false),
                    CorrectClassifications = table.Column<int>(type: "int", nullable: false),
                    AccuracyPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AverageConfidence = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FeedbackCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationAccuracyLog", x => x.LogID);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceHeader",
                columns: table => new
                {
                    InvoiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Odometer = table.Column<int>(type: "int", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPartsCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BlobFileUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExtractedData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Approved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OriginalVehicleLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OriginalOdometerLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OriginalInvoiceLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NormalizationVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceHeader", x => x.InvoiceID);
                    table.CheckConstraint("CK_InvoiceHeader_ConfidenceScore", "ConfidenceScore IS NULL OR (ConfidenceScore >= 0 AND ConfidenceScore <= 100)");
                    table.CheckConstraint("CK_InvoiceHeader_Odometer", "Odometer IS NULL OR Odometer >= 0");
                    table.CheckConstraint("CK_InvoiceHeader_TotalCost", "TotalCost >= 0");
                    table.CheckConstraint("CK_InvoiceHeader_TotalLaborCost", "TotalLaborCost >= 0");
                    table.CheckConstraint("CK_InvoiceHeader_TotalPartsCost", "TotalPartsCost >= 0");
                });

            migrationBuilder.CreateTable(
                name: "FieldNormalizationFeedback",
                columns: table => new
                {
                    NormalizationFeedbackID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceID = table.Column<int>(type: "int", nullable: false),
                    OriginalLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentNormalization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpectedNormalization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FeedbackDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    NormalizationVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Applied = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldNormalizationFeedback", x => x.NormalizationFeedbackID);
                    table.ForeignKey(
                        name: "FK_FieldNormalizationFeedback_InvoiceHeader_InvoiceID",
                        column: x => x.InvoiceID,
                        principalTable: "InvoiceHeader",
                        principalColumn: "InvoiceID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    LineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceID = table.Column<int>(type: "int", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalLineCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClassifiedCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClassificationConfidence = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ClassificationMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClassificationVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OriginalCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExtractionConfidence = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.LineID);
                    table.CheckConstraint("CK_InvoiceLines_ClassificationConfidence", "ClassificationConfidence IS NULL OR (ClassificationConfidence >= 0 AND ClassificationConfidence <= 100)");
                    table.CheckConstraint("CK_InvoiceLines_ExtractionConfidence", "ExtractionConfidence IS NULL OR (ExtractionConfidence >= 0 AND ExtractionConfidence <= 100)");
                    table.CheckConstraint("CK_InvoiceLines_LineNumber", "LineNumber > 0");
                    table.CheckConstraint("CK_InvoiceLines_Quantity", "Quantity > 0");
                    table.CheckConstraint("CK_InvoiceLines_TotalLineCost", "TotalLineCost >= 0");
                    table.CheckConstraint("CK_InvoiceLines_UnitCost", "UnitCost >= 0");
                    table.ForeignKey(
                        name: "FK_InvoiceLines_InvoiceHeader_InvoiceID",
                        column: x => x.InvoiceID,
                        principalTable: "InvoiceHeader",
                        principalColumn: "InvoiceID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassificationFeedback",
                columns: table => new
                {
                    FeedbackID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineID = table.Column<int>(type: "int", nullable: false),
                    InvoiceID = table.Column<int>(type: "int", nullable: false),
                    OriginalClassification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CorrectedClassification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalConfidence = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    UserComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FeedbackDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ModelVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Applied = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationFeedback", x => x.FeedbackID);
                    table.ForeignKey(
                        name: "FK_ClassificationFeedback_InvoiceHeader_InvoiceID",
                        column: x => x.InvoiceID,
                        principalTable: "InvoiceHeader",
                        principalColumn: "InvoiceID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassificationFeedback_InvoiceLines_LineID",
                        column: x => x.LineID,
                        principalTable: "InvoiceLines",
                        principalColumn: "LineID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationAccuracyLog_ClassificationType",
                table: "ClassificationAccuracyLog",
                column: "ClassificationType");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationAccuracyLog_DatePeriod",
                table: "ClassificationAccuracyLog",
                column: "DatePeriod");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationAccuracyLog_ModelVersion",
                table: "ClassificationAccuracyLog",
                column: "ModelVersion");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationFeedback_FeedbackDate",
                table: "ClassificationFeedback",
                column: "FeedbackDate");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationFeedback_InvoiceID",
                table: "ClassificationFeedback",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationFeedback_LineID",
                table: "ClassificationFeedback",
                column: "LineID");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationFeedback_UserID",
                table: "ClassificationFeedback",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_FieldNormalizationFeedback_FeedbackDate",
                table: "FieldNormalizationFeedback",
                column: "FeedbackDate");

            migrationBuilder.CreateIndex(
                name: "IX_FieldNormalizationFeedback_FieldType",
                table: "FieldNormalizationFeedback",
                column: "FieldType");

            migrationBuilder.CreateIndex(
                name: "IX_FieldNormalizationFeedback_InvoiceID",
                table: "FieldNormalizationFeedback",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_FieldNormalizationFeedback_UserID",
                table: "FieldNormalizationFeedback",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHeader_CreatedAt",
                table: "InvoiceHeader",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHeader_InvoiceDate",
                table: "InvoiceHeader",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHeader_InvoiceNumber",
                table: "InvoiceHeader",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHeader_VehicleID",
                table: "InvoiceHeader",
                column: "VehicleID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_Category",
                table: "InvoiceLines",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceID",
                table: "InvoiceLines",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceID_LineNumber",
                table: "InvoiceLines",
                columns: new[] { "InvoiceID", "LineNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_PartNumber",
                table: "InvoiceLines",
                column: "PartNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassificationAccuracyLog");

            migrationBuilder.DropTable(
                name: "ClassificationFeedback");

            migrationBuilder.DropTable(
                name: "FieldNormalizationFeedback");

            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "InvoiceHeader");
        }
    }
}
