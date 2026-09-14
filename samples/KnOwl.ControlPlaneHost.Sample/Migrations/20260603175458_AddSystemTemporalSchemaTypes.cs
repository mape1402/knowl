using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemTemporalSchemaTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SchemaTypeDefinitions",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "IsSystem", "Name", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111118"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "System schema type: Date", true, true, "Date", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-1111-1111-1111-111111111119"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "System schema type: DateTime", true, true, "DateTime", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-1111-1111-1111-111111111120"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "System schema type: TimeSpan", true, true, "TimeSpan", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "SchemaTypeVersions",
                columns: new[] { "Id", "BaseType", "Comment", "CreatedAtUtc", "IsActive", "JsonSchema", "SchemaTypeDefinitionId", "UpdatedAtUtc", "VersionNumber" },
                values: new object[,]
                {
                    { new Guid("21111111-1111-1111-1111-111111111118"), "string", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"string\",\"pattern\":\"^\\\\d{4}-\\\\d{2}-\\\\d{2}$\"}", new Guid("11111111-1111-1111-1111-111111111118"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" },
                    { new Guid("21111111-1111-1111-1111-111111111119"), "string", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"string\",\"pattern\":\"^\\\\d{4}-\\\\d{2}-\\\\d{2}T\\\\d{2}:\\\\d{2}:\\\\d{2}(?:\\\\.\\\\d{1,7})?(?:Z|[+-]\\\\d{2}:\\\\d{2})?$\"}", new Guid("11111111-1111-1111-1111-111111111119"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" },
                    { new Guid("21111111-1111-1111-1111-111111111120"), "string", null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "{\"type\":\"string\",\"pattern\":\"^(?:\\\\d+\\\\.)?\\\\d{2}:\\\\d{2}:\\\\d{2}(?:\\\\.\\\\d{1,7})?$\"}", new Guid("11111111-1111-1111-1111-111111111120"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SchemaTypeVersions",
                keyColumn: "Id",
                keyValue: new Guid("21111111-1111-1111-1111-111111111118"));

            migrationBuilder.DeleteData(
                table: "SchemaTypeVersions",
                keyColumn: "Id",
                keyValue: new Guid("21111111-1111-1111-1111-111111111119"));

            migrationBuilder.DeleteData(
                table: "SchemaTypeVersions",
                keyColumn: "Id",
                keyValue: new Guid("21111111-1111-1111-1111-111111111120"));

            migrationBuilder.DeleteData(
                table: "SchemaTypeDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111118"));

            migrationBuilder.DeleteData(
                table: "SchemaTypeDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111119"));

            migrationBuilder.DeleteData(
                table: "SchemaTypeDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111120"));
        }
    }
}
