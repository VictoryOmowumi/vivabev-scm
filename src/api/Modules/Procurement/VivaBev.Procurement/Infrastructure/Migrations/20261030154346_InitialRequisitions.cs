using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivaBev.Procurement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialRequisitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "procurement");

            migrationBuilder.CreateTable(
                name: "requisitions",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostCentreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliverToSiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequiredBy = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    EstimatedTotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requisitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "requisition_lines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    LineType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UomCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EstimatedUnitPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requisition_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_requisition_lines_requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalSchema: "procurement",
                        principalTable: "requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_requisition_lines_RequisitionId_LineNo",
                schema: "procurement",
                table: "requisition_lines",
                columns: new[] { "RequisitionId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_requisitions_Number",
                schema: "procurement",
                table: "requisitions",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_requisitions_RequesterId",
                schema: "procurement",
                table: "requisitions",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_requisitions_Status",
                schema: "procurement",
                table: "requisitions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "requisition_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "requisitions",
                schema: "procurement");
        }
    }
}
