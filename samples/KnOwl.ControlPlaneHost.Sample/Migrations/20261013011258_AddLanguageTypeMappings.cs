using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageTypeMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LanguageTypeBundles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssemblyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssemblyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssemblyDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssemblyVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LanguageKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BundleJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageTypeBundles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LanguageTypeMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanguageKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DocumentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageTypeMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LanguageTypeBundles_AssemblyId",
                table: "LanguageTypeBundles",
                column: "AssemblyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LanguageTypeBundles_LanguageKey_IsActive",
                table: "LanguageTypeBundles",
                columns: new[] { "LanguageKey", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_LanguageTypeMappings_Key",
                table: "LanguageTypeMappings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LanguageTypeMappings_LanguageKey_IsActive",
                table: "LanguageTypeMappings",
                columns: new[] { "LanguageKey", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LanguageTypeBundles");

            migrationBuilder.DropTable(
                name: "LanguageTypeMappings");
        }
    }
}
