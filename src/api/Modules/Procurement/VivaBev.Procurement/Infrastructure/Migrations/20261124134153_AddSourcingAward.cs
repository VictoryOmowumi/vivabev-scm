using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivaBev.Procurement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourcingAward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AwardJustification",
                schema: "procurement",
                table: "sourcing_events",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AwardQuotationId",
                schema: "procurement",
                table: "sourcing_events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AwardRecommendedAt",
                schema: "procurement",
                table: "sourcing_events",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AwardRejectionReason",
                schema: "procurement",
                table: "sourcing_events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AwardedAt",
                schema: "procurement",
                table: "sourcing_events",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "evaluation_criteria",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcingEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_criteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluation_criteria_sourcing_events_SourcingEventId",
                        column: x => x.SourcingEventId,
                        principalSchema: "procurement",
                        principalTable: "sourcing_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_scores",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcingEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluation_scores_sourcing_events_SourcingEventId",
                        column: x => x.SourcingEventId,
                        principalSchema: "procurement",
                        principalTable: "sourcing_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_criteria_SourcingEventId_SortOrder",
                schema: "procurement",
                table: "evaluation_criteria",
                columns: new[] { "SourcingEventId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_scores_CriterionId_QuotationId",
                schema: "procurement",
                table: "evaluation_scores",
                columns: new[] { "CriterionId", "QuotationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_scores_SourcingEventId",
                schema: "procurement",
                table: "evaluation_scores",
                column: "SourcingEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evaluation_criteria",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "evaluation_scores",
                schema: "procurement");

            migrationBuilder.DropColumn(
                name: "AwardJustification",
                schema: "procurement",
                table: "sourcing_events");

            migrationBuilder.DropColumn(
                name: "AwardQuotationId",
                schema: "procurement",
                table: "sourcing_events");

            migrationBuilder.DropColumn(
                name: "AwardRecommendedAt",
                schema: "procurement",
                table: "sourcing_events");

            migrationBuilder.DropColumn(
                name: "AwardRejectionReason",
                schema: "procurement",
                table: "sourcing_events");

            migrationBuilder.DropColumn(
                name: "AwardedAt",
                schema: "procurement",
                table: "sourcing_events");
        }
    }
}
