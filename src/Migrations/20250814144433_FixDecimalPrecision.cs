using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleMaintenanceInvoiceSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
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
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.LineID);
                    table.CheckConstraint("CK_InvoiceLines_ConfidenceScore", "ConfidenceScore IS NULL OR (ConfidenceScore >= 0 AND ConfidenceScore <= 100)");
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
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "InvoiceHeader");
        }
    }
}
