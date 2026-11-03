using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivaBev.Approvals.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOverBudgetApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OverBudget",
                schema: "approvals",
                table: "requests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OverBudgetOnly",
                schema: "approvals",
                table: "policy_steps",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverBudget",
                schema: "approvals",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "OverBudgetOnly",
                schema: "approvals",
                table: "policy_steps");
        }
    }
}
