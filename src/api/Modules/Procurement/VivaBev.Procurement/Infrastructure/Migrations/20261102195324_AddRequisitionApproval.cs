using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivaBev.Procurement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequisitionApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data migration: 'Submitted' no longer exists. Those requisitions never got an approval
            // request, so they return to Draft; resubmitting starts one properly.
            migrationBuilder.Sql(
                "UPDATE procurement.requisitions SET Status = 'Draft', SubmittedAt = NULL WHERE Status = 'Submitted';");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                schema: "procurement",
                table: "requisitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "procurement",
                table: "requisitions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "procurement",
                table: "requisitions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "procurement",
                table: "requisitions");

            migrationBuilder.Sql(
                "UPDATE procurement.requisitions SET Status = 'Submitted' WHERE Status IN ('PendingApproval', 'Approved');");
        }
    }
}
