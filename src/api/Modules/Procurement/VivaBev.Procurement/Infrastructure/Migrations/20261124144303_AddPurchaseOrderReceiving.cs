using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivaBev.Procurement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderReceiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "QuantityReceived",
                schema: "procurement",
                table: "purchase_order_lines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityReceived",
                schema: "procurement",
                table: "purchase_order_lines");
        }
    }
}
