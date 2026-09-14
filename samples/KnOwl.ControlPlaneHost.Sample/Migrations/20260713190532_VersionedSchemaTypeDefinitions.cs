using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class VersionedSchemaTypeDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SchemaTypeVersions_BaseType_IsActive",
                table: "SchemaTypeVersions");

            migrationBuilder.DropIndex(
                name: "IX_SchemaTypeDefinitions_Name",
                table: "SchemaTypeDefinitions");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "SchemaTypeDefinitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefinitionJson",
                table: "SchemaTypeVersions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.Sql(
                """
                UPDATE [SchemaTypeDefinitions]
                SET [Key] = [Name]
                WHERE [Key] = N'';
                """);

            migrationBuilder.Sql(
                """
                UPDATE version_row
                SET [DefinitionJson] = CONCAT(
                    N'{"key":"', STRING_ESCAPE(definition.[Key], 'json'),
                    N'","name":"', STRING_ESCAPE(definition.[Name], 'json'),
                    N'","description":"', STRING_ESCAPE(COALESCE(definition.[Description], N''), 'json'),
                    N'","version":"', STRING_ESCAPE(version_row.[VersionNumber], 'json'),
                    N'","baseType":"', STRING_ESCAPE(version_row.[BaseType], 'json'),
                    N'","comment":"', STRING_ESCAPE(COALESCE(version_row.[Comment], N''), 'json'),
                    N'","schema":', CASE WHEN ISJSON(version_row.[JsonSchema]) = 1 THEN version_row.[JsonSchema] ELSE N'{}' END,
                    N'}')
                FROM [SchemaTypeVersions] version_row
                INNER JOIN [SchemaTypeDefinitions] definition
                    ON definition.[Id] = version_row.[SchemaTypeDefinitionId];
                """);

            migrationBuilder.DropColumn(
                name: "BaseType",
                table: "SchemaTypeVersions");

            migrationBuilder.DropColumn(
                name: "JsonSchema",
                table: "SchemaTypeVersions");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaTypeVersions_IsActive",
                table: "SchemaTypeVersions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaTypeDefinitions_Key",
                table: "SchemaTypeDefinitions",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SchemaTypeVersions_IsActive",
                table: "SchemaTypeVersions");

            migrationBuilder.DropIndex(
                name: "IX_SchemaTypeDefinitions_Key",
                table: "SchemaTypeDefinitions");

            migrationBuilder.AddColumn<string>(
                name: "BaseType",
                table: "SchemaTypeVersions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JsonSchema",
                table: "SchemaTypeVersions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.Sql(
                """
                UPDATE [SchemaTypeVersions]
                SET [BaseType] = COALESCE(JSON_VALUE([DefinitionJson], '$.baseType'), N'string'),
                    [JsonSchema] = COALESCE(JSON_QUERY([DefinitionJson], '$.schema'), N'{}')
                WHERE ISJSON([DefinitionJson]) = 1;
                """);

            migrationBuilder.DropColumn(
                name: "DefinitionJson",
                table: "SchemaTypeVersions");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "SchemaTypeDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaTypeVersions_BaseType_IsActive",
                table: "SchemaTypeVersions",
                columns: new[] { "BaseType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SchemaTypeDefinitions_Name",
                table: "SchemaTypeDefinitions",
                column: "Name",
                unique: true);
        }
    }
}
