using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemTimeSchemaType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SchemaTypeDefinitions",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "IsSystem", "Key", "Name", "UpdatedAtUtc" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111121"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "System schema type: Time", true, true, "Time", "Time", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "SchemaTypeVersions",
                columns: new[] { "Id", "Comment", "CreatedAtUtc", "DefinitionJson", "IsActive", "SchemaTypeDefinitionId", "UpdatedAtUtc", "VersionNumber" },
                values: new object[] { new Guid("21111111-1111-1111-1111-111111111121"), null, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "{\"key\":\"Time\",\"name\":\"Time\",\"description\":\"System schema type: Time\",\"version\":\"1.0.0\",\"baseType\":\"string\",\"comment\":\"\",\"schema\":{\"type\":\"string\",\"pattern\":\"^\\\\d{2}:\\\\d{2}:\\\\d{2}(?:\\\\.\\\\d{1,7})?$\"}}", true, new Guid("11111111-1111-1111-1111-111111111121"), new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "1.0.0" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SchemaTypeVersions",
                keyColumn: "Id",
                keyValue: new Guid("21111111-1111-1111-1111-111111111121"));

            migrationBuilder.DeleteData(
                table: "SchemaTypeDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111121"));
        }
    }
}
