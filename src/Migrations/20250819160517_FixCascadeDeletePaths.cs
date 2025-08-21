using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleMaintenanceInvoiceSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeDeletePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassificationFeedback_InvoiceHeader_InvoiceID",
                table: "ClassificationFeedback");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassificationFeedback_InvoiceHeader_InvoiceID",
                table: "ClassificationFeedback",
                column: "InvoiceID",
                principalTable: "InvoiceHeader",
                principalColumn: "InvoiceID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassificationFeedback_InvoiceHeader_InvoiceID",
                table: "ClassificationFeedback");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassificationFeedback_InvoiceHeader_InvoiceID",
                table: "ClassificationFeedback",
                column: "InvoiceID",
                principalTable: "InvoiceHeader",
                principalColumn: "InvoiceID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
