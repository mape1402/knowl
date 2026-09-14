using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNullSchemaType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SchemaTypeVersions",
                keyColumn: "Id",
                keyValue: new Guid("21111111-1111-1111-1111-111111111117"));

            migrationBuilder.DeleteData(
                table: "SchemaTypeDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111117"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SchemaTypeDefinitions",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "IsSystem", "Name", "UpdatedAtUtc" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111117"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "JSON Schema basic type: null", true, true, "null", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "SchemaTypeVersions",
                columns: new[] { "Id", "BaseType", "Comment", "CreatedAtUtc", "IsActive", "JsonSchema", "SchemaTypeDefinitionId", "UpdatedAtUtc", "VersionNumber" },
                values: new object[] { new Guid("21111111-1111-1111-1111-111111111117"), "null", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"null\"}", new Guid("11111111-1111-1111-1111-111111111117"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" });
        }
    }
}
