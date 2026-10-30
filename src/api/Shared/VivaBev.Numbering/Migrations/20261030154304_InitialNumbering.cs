using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VivaBev.Numbering.Migrations
{
    /// <inheritdoc />
    public partial class InitialNumbering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "numbering");

            migrationBuilder.CreateTable(
                name: "document_sequences",
                schema: "numbering",
                columns: table => new
                {
                    Prefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    NextValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_sequences", x => new { x.Prefix, x.FiscalYear });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_sequences",
                schema: "numbering");
        }
    }
}
