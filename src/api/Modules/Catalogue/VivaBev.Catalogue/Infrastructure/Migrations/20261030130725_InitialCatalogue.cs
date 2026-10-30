using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivaBev.Catalogue.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalogue");

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "catalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categories_categories_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "catalogue",
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "units_of_measure",
                schema: "catalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Dimension = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_units_of_measure", x => x.Id);
                    table.UniqueConstraint("AK_units_of_measure_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "catalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ItemType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaseUomCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsStocked = table.Column<bool>(type: "bit", nullable: false),
                    IsLotControlled = table.Column<bool>(type: "bit", nullable: false),
                    ShelfLifeDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_items_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalogue",
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_units_of_measure_BaseUomCode",
                        column: x => x.BaseUomCode,
                        principalSchema: "catalogue",
                        principalTable: "units_of_measure",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "uom_conversions",
                schema: "catalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromUomCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ToUomCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Factor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uom_conversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uom_conversions_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "catalogue",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_uom_conversions_units_of_measure_FromUomCode",
                        column: x => x.FromUomCode,
                        principalSchema: "catalogue",
                        principalTable: "units_of_measure",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_uom_conversions_units_of_measure_ToUomCode",
                        column: x => x.ToUomCode,
                        principalSchema: "catalogue",
                        principalTable: "units_of_measure",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categories_Code",
                schema: "catalogue",
                table: "categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_ParentId",
                schema: "catalogue",
                table: "categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_items_BaseUomCode",
                schema: "catalogue",
                table: "items",
                column: "BaseUomCode");

            migrationBuilder.CreateIndex(
                name: "IX_items_CategoryId",
                schema: "catalogue",
                table: "items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_items_Code",
                schema: "catalogue",
                table: "items",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uom_conversions_FromUomCode",
                schema: "catalogue",
                table: "uom_conversions",
                column: "FromUomCode");

            migrationBuilder.CreateIndex(
                name: "IX_uom_conversions_ItemId_FromUomCode_ToUomCode",
                schema: "catalogue",
                table: "uom_conversions",
                columns: new[] { "ItemId", "FromUomCode", "ToUomCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uom_conversions_ToUomCode",
                schema: "catalogue",
                table: "uom_conversions",
                column: "ToUomCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "uom_conversions",
                schema: "catalogue");

            migrationBuilder.DropTable(
                name: "items",
                schema: "catalogue");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "catalogue");

            migrationBuilder.DropTable(
                name: "units_of_measure",
                schema: "catalogue");
        }
    }
}
