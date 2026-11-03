using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivaBev.Procurement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequisitionBudgetCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BudgetAvailableAtSubmit",
                schema: "procurement",
                table: "requisitions",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BudgetFiscalYear",
                schema: "procurement",
                table: "requisitions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudgetStatus",
                schema: "procurement",
                table: "requisitions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                // Existing rows were never checked. "" isn't a valid BudgetStatus and would fail to load.
                defaultValue: "NotChecked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetAvailableAtSubmit",
                schema: "procurement",
                table: "requisitions");

            migrationBuilder.DropColumn(
                name: "BudgetFiscalYear",
                schema: "procurement",
                table: "requisitions");

            migrationBuilder.DropColumn(
                name: "BudgetStatus",
                schema: "procurement",
                table: "requisitions");
        }
    }
}
