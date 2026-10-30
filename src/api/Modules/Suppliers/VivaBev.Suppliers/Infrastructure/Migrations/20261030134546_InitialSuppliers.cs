using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivaBev.Suppliers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "suppliers");

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TradingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    DefaultCurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    PaymentTermsDays = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_addresses",
                schema: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_addresses_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "suppliers",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_categories",
                schema: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_categories_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "suppliers",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_contacts",
                schema: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_contacts_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "suppliers",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_addresses_SupplierId",
                schema: "suppliers",
                table: "supplier_addresses",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_categories_CategoryId",
                schema: "suppliers",
                table: "supplier_categories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_categories_SupplierId_CategoryId",
                schema: "suppliers",
                table: "supplier_categories",
                columns: new[] { "SupplierId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contacts_SupplierId",
                schema: "suppliers",
                table: "supplier_contacts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_Code",
                schema: "suppliers",
                table: "suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_Status",
                schema: "suppliers",
                table: "suppliers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_TaxId",
                schema: "suppliers",
                table: "suppliers",
                column: "TaxId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_addresses",
                schema: "suppliers");

            migrationBuilder.DropTable(
                name: "supplier_categories",
                schema: "suppliers");

            migrationBuilder.DropTable(
                name: "supplier_contacts",
                schema: "suppliers");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "suppliers");
        }
    }
}
