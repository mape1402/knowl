using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.RuntimeHost.Sample.Migrations.RuntimeStorage
{
    /// <inheritdoc />
    public partial class ReplaceRuntimeContractTopicsWithKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_Topic_DeployedAtUtc",
                table: "RuntimeContractArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_Topic_VersionNumber",
                table: "RuntimeContractArtifacts");

            migrationBuilder.AddColumn<string>(
                name: "ContractKey",
                table: "RuntimeContractArtifacts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [a]
                SET [ContractKey] = COALESCE(
                    LEFT(NULLIF(LTRIM(RTRIM(JSON_VALUE(
                        CASE WHEN ISJSON([a].[PayloadSchemaJson]) = 1 THEN [a].[PayloadSchemaJson] ELSE N'{}' END,
                        '$.request.key'))), ''), 100),
                    LEFT(NULLIF(LTRIM(RTRIM(JSON_VALUE(
                        CASE WHEN ISJSON([a].[PayloadSchemaJson]) = 1 THEN [a].[PayloadSchemaJson] ELSE N'{}' END,
                        '$.key'))), ''), 100),
                    CONCAT('runtime-artifact-', LOWER(CONVERT(char(36), [a].[Id]))))
                FROM [RuntimeContractArtifacts] AS [a];
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ContractKey",
                table: "RuntimeContractArtifacts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "RuntimeContractArtifacts");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_ContractKey_DeployedAtUtc",
                table: "RuntimeContractArtifacts",
                columns: new[] { "ArtifactType", "ContractKey", "DeployedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_ContractKey_VersionNumber",
                table: "RuntimeContractArtifacts",
                columns: new[] { "ArtifactType", "ContractKey", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_ContractKey_DeployedAtUtc",
                table: "RuntimeContractArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_ContractKey_VersionNumber",
                table: "RuntimeContractArtifacts");

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "RuntimeContractArtifacts",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [RuntimeContractArtifacts]
                SET [Topic] = LEFT(NULLIF(LTRIM(RTRIM([ContractKey])), ''), 70);

                UPDATE [RuntimeContractArtifacts]
                SET [Topic] = CONCAT('runtime-artifact-', LOWER(CONVERT(char(36), [DefinitionId])))
                WHERE [Topic] IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Topic",
                table: "RuntimeContractArtifacts",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(70)",
                oldMaxLength: 70,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ContractKey",
                table: "RuntimeContractArtifacts");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_Topic_DeployedAtUtc",
                table: "RuntimeContractArtifacts",
                columns: new[] { "ArtifactType", "Topic", "DeployedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_Topic_VersionNumber",
                table: "RuntimeContractArtifacts",
                columns: new[] { "ArtifactType", "Topic", "VersionNumber" },
                unique: true);
        }
    }
}
