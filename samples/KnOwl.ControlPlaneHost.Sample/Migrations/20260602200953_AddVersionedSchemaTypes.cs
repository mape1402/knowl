using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedSchemaTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchemaTypeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaTypeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchemaTypeVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemaTypeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    BaseType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JsonSchema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaTypeVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchemaTypeVersions_SchemaTypeDefinitions_SchemaTypeDefinitionId",
                        column: x => x.SchemaTypeDefinitionId,
                        principalTable: "SchemaTypeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SchemaTypeDefinitions",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "IsSystem", "Name", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "JSON Schema basic type: string", true, true, "string", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-1111-1111-1111-111111111112"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "JSON Schema basic type: number", true, true, "number", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-1111-1111-1111-111111111113"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "JSON Schema basic type: integer", true, true, "integer", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-1111-1111-1111-111111111114"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "JSON Schema basic type: boolean", true, true, "boolean", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-1111-1111-1111-111111111115"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "JSON Schema basic type: object", true, true, "object", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-1111-1111-1111-111111111116"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "JSON Schema basic type: array", true, true, "array", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-1111-1111-1111-111111111117"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "JSON Schema basic type: null", true, true, "null", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "SchemaTypeVersions",
                columns: new[] { "Id", "BaseType", "Comment", "CreatedAtUtc", "IsActive", "JsonSchema", "SchemaTypeDefinitionId", "UpdatedAtUtc", "VersionNumber" },
                values: new object[,]
                {
                    { new Guid("21111111-1111-1111-1111-111111111111"), "string", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"string\"}", new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" },
                    { new Guid("21111111-1111-1111-1111-111111111112"), "number", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"number\"}", new Guid("11111111-1111-1111-1111-111111111112"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" },
                    { new Guid("21111111-1111-1111-1111-111111111113"), "integer", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"integer\"}", new Guid("11111111-1111-1111-1111-111111111113"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" },
                    { new Guid("21111111-1111-1111-1111-111111111114"), "boolean", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"boolean\"}", new Guid("11111111-1111-1111-1111-111111111114"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" },
                    { new Guid("21111111-1111-1111-1111-111111111115"), "object", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"object\"}", new Guid("11111111-1111-1111-1111-111111111115"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" },
                    { new Guid("21111111-1111-1111-1111-111111111116"), "array", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"array\"}", new Guid("11111111-1111-1111-1111-111111111116"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" },
                    { new Guid("21111111-1111-1111-1111-111111111117"), "null", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"null\"}", new Guid("11111111-1111-1111-1111-111111111117"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchemaTypeDefinitions_IsActive",
                table: "SchemaTypeDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaTypeDefinitions_Name",
                table: "SchemaTypeDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchemaTypeVersions_BaseType_IsActive",
                table: "SchemaTypeVersions",
                columns: new[] { "BaseType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SchemaTypeVersions_SchemaTypeDefinitionId_VersionNumber",
                table: "SchemaTypeVersions",
                columns: new[] { "SchemaTypeDefinitionId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchemaTypeVersions");

            migrationBuilder.DropTable(
                name: "SchemaTypeDefinitions");
        }
    }
}
