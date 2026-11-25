using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivaBev.Procurement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseOrderCloseOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                schema: "procurement",
                table: "purchase_orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedBy",
                schema: "procurement",
                table: "purchase_orders",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAt",
                schema: "procurement",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ClosedBy",
                schema: "procurement",
                table: "purchase_orders");
        }
    }
}
