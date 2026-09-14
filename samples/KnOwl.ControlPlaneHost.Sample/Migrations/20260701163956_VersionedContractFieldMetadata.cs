using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class VersionedContractFieldMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContractFieldMetadataDefinitions_IsActive_SortOrder",
                table: "ContractFieldMetadataDefinitions");

            migrationBuilder.CreateTable(
                name: "ContractFieldMetadataVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractFieldMetadataDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractFieldMetadataVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractFieldMetadataVersions_ContractFieldMetadataDefinitions_ContractFieldMetadataDefinitionId",
                        column: x => x.ContractFieldMetadataDefinitionId,
                        principalTable: "ContractFieldMetadataDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractFieldMetadataDefinitions_IsActive",
                table: "ContractFieldMetadataDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ContractFieldMetadataVersions_ContractFieldMetadataDefinitionId_VersionNumber",
                table: "ContractFieldMetadataVersions",
                columns: new[] { "ContractFieldMetadataDefinitionId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractFieldMetadataVersions_IsActive",
                table: "ContractFieldMetadataVersions",
                column: "IsActive");

            migrationBuilder.Sql(
                """
                INSERT INTO [ContractFieldMetadataVersions]
                    ([Id], [ContractFieldMetadataDefinitionId], [VersionNumber], [Comment], [DefinitionJson], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT
                    NEWID(),
                    [Id],
                    N'1.0.0',
                    NULL,
                    (
                        SELECT
                            [Key] AS [key],
                            [Name] AS [name],
                            COALESCE([Description], N'') AS [description],
                            N'1.0.0' AS [version],
                            N'' AS [versionComment],
                            [DataType] AS [dataType],
                            JSON_QUERY(N'["Field"]') AS [appliesTo],
                            [IsRequired] AS [isRequired],
                            [IsActive] AS [isActive],
                            CASE
                                WHEN ISJSON([ValidationJson]) = 1 THEN JSON_QUERY([ValidationJson])
                                ELSE JSON_QUERY(N'{}')
                            END AS [validation]
                        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                    ),
                    [IsActive],
                    [CreatedAtUtc],
                    [UpdatedAtUtc]
                FROM [ContractFieldMetadataDefinitions];
                """);

            migrationBuilder.DropColumn(
                name: "AppliesToJson",
                table: "ContractFieldMetadataDefinitions");

            migrationBuilder.DropColumn(
                name: "DataType",
                table: "ContractFieldMetadataDefinitions");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "ContractFieldMetadataDefinitions");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ContractFieldMetadataDefinitions");

            migrationBuilder.DropColumn(
                name: "ValidationJson",
                table: "ContractFieldMetadataDefinitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContractFieldMetadataDefinitions_IsActive",
                table: "ContractFieldMetadataDefinitions");

            migrationBuilder.AddColumn<string>(
                name: "AppliesToJson",
                table: "ContractFieldMetadataDefinitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DataType",
                table: "ContractFieldMetadataDefinitions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "ContractFieldMetadataDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ContractFieldMetadataDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ValidationJson",
                table: "ContractFieldMetadataDefinitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE definition
                SET
                    [DataType] = COALESCE(JSON_VALUE(version_row.[DefinitionJson], '$.dataType'), N'string'),
                    [AppliesToJson] = N'["events","commands"]',
                    [IsRequired] = CASE WHEN JSON_VALUE(version_row.[DefinitionJson], '$.isRequired') = N'true' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,
                    [ValidationJson] = COALESCE(JSON_QUERY(version_row.[DefinitionJson], '$.validation'), N'{}')
                FROM [ContractFieldMetadataDefinitions] definition
                OUTER APPLY
                (
                    SELECT TOP 1 [DefinitionJson]
                    FROM [ContractFieldMetadataVersions] version
                    WHERE version.[ContractFieldMetadataDefinitionId] = definition.[Id]
                    ORDER BY version.[CreatedAtUtc] DESC
                ) version_row;
                """);

            migrationBuilder.DropTable(
                name: "ContractFieldMetadataVersions");

            migrationBuilder.CreateIndex(
                name: "IX_ContractFieldMetadataDefinitions_IsActive_SortOrder",
                table: "ContractFieldMetadataDefinitions",
                columns: new[] { "IsActive", "SortOrder" });
        }
    }
}
