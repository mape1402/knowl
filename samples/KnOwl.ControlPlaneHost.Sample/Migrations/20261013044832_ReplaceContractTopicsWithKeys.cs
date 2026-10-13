using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.ControlPlaneHost.Sample.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceContractTopicsWithKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_Topic_VersionNumber",
                table: "RuntimeContractArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_Events_Topic",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_ContractArtifacts_ArtifactType_Topic_VersionNumber",
                table: "ContractArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_Commands_Topic",
                table: "Commands");

            migrationBuilder.AddColumn<string>(
                name: "ContractKey",
                table: "RuntimeContractArtifacts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractKey",
                table: "ContractArtifacts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Commands",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [v]
                SET [PayloadSchemaJson] = JSON_MODIFY(
                    CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                    '$.key',
                    [derived].[SchemaKey])
                FROM [EventVersions] AS [v]
                INNER JOIN [Events] AS [e] ON [v].[EventDefinitionId] = [e].[Id]
                CROSS APPLY (
                    SELECT LEFT(COALESCE(
                        NULLIF(LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM([e].[Name])), N' ', N'-'), N'.', N'-'), N'_', N'-'), N'/', N'-'), N'\', N'-')), N''),
                        CONCAT(N'event-', LOWER(CONVERT(char(36), [e].[Id])))), 100) AS [SchemaKey]
                ) AS [derived]
                WHERE NULLIF(LTRIM(RTRIM(JSON_VALUE(
                    CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                    '$.key'))), '') IS NULL;

                IF EXISTS (
                    SELECT 1
                    FROM (
                        SELECT [v].[EventDefinitionId]
                        FROM [EventVersions] AS [v]
                        WHERE NULLIF(LTRIM(RTRIM(JSON_VALUE(
                            CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                            '$.key'))), '') IS NOT NULL
                        GROUP BY [v].[EventDefinitionId]
                        HAVING COUNT(DISTINCT NULLIF(LTRIM(RTRIM(JSON_VALUE(
                            CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                            '$.key'))), '')) > 1
                    ) AS [conflicts]
                )
                BEGIN
                    THROW 51001, 'Cannot migrate Events.Key because at least one event has conflicting payload schema keys across versions.', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM [EventVersions] AS [v]
                    WHERE LEN(NULLIF(LTRIM(RTRIM(JSON_VALUE(
                        CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                        '$.key'))), '')) > 100
                )
                BEGIN
                    THROW 51002, 'Cannot migrate Events.Key because at least one event payload schema key is longer than 100 characters.', 1;
                END;

                UPDATE [e]
                SET [Key] = [keys].[SchemaKey]
                FROM [Events] AS [e]
                CROSS APPLY (
                    SELECT TOP(1)
                        NULLIF(LTRIM(RTRIM(JSON_VALUE(
                            CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                            '$.key'))), '') AS [SchemaKey]
                    FROM [EventVersions] AS [v]
                    WHERE [v].[EventDefinitionId] = [e].[Id]
                        AND NULLIF(LTRIM(RTRIM(JSON_VALUE(
                            CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                            '$.key'))), '') IS NOT NULL
                    ORDER BY [v].[CreatedAtUtc] DESC, [v].[Id] DESC
                ) AS [keys];
                """);

            migrationBuilder.Sql("""
                UPDATE [v]
                SET [PayloadSchemaJson] = JSON_MODIFY(
                    CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                    '$.key',
                    [derived].[SchemaKey])
                FROM [CommandVersions] AS [v]
                INNER JOIN [Commands] AS [c] ON [v].[CommandDefinitionId] = [c].[Id]
                CROSS APPLY (
                    SELECT LEFT(COALESCE(
                        NULLIF(LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM([c].[Name])), N' ', N'-'), N'.', N'-'), N'_', N'-'), N'/', N'-'), N'\', N'-')), N''),
                        CONCAT(N'command-', LOWER(CONVERT(char(36), [c].[Id])))), 100) AS [SchemaKey]
                ) AS [derived]
                WHERE NULLIF(LTRIM(RTRIM(JSON_VALUE(
                    CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                    '$.key'))), '') IS NULL;

                IF EXISTS (
                    SELECT 1
                    FROM (
                        SELECT [v].[CommandDefinitionId]
                        FROM [CommandVersions] AS [v]
                        WHERE NULLIF(LTRIM(RTRIM(JSON_VALUE(
                            CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                            '$.key'))), '') IS NOT NULL
                        GROUP BY [v].[CommandDefinitionId]
                        HAVING COUNT(DISTINCT NULLIF(LTRIM(RTRIM(JSON_VALUE(
                            CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                            '$.key'))), '')) > 1
                    ) AS [conflicts]
                )
                BEGIN
                    THROW 51004, 'Cannot migrate Commands.Key because at least one command has conflicting request payload schema keys across versions.', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM [CommandVersions] AS [v]
                    WHERE LEN(NULLIF(LTRIM(RTRIM(JSON_VALUE(
                        CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                        '$.key'))), '')) > 100
                )
                BEGIN
                    THROW 51005, 'Cannot migrate Commands.Key because at least one command request payload schema key is longer than 100 characters.', 1;
                END;

                UPDATE [c]
                SET [Key] = [keys].[SchemaKey]
                FROM [Commands] AS [c]
                CROSS APPLY (
                    SELECT TOP(1)
                        NULLIF(LTRIM(RTRIM(JSON_VALUE(
                            CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                            '$.key'))), '') AS [SchemaKey]
                    FROM [CommandVersions] AS [v]
                    WHERE [v].[CommandDefinitionId] = [c].[Id]
                        AND NULLIF(LTRIM(RTRIM(JSON_VALUE(
                            CASE WHEN ISJSON([v].[PayloadSchemaJson]) = 1 THEN [v].[PayloadSchemaJson] ELSE N'{}' END,
                            '$.key'))), '') IS NOT NULL
                    ORDER BY [v].[CreatedAtUtc] DESC, [v].[Id] DESC
                ) AS [keys];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM [ContractArtifacts] AS [a]
                    WHERE LEN(NULLIF(LTRIM(RTRIM(JSON_VALUE(
                        CASE WHEN ISJSON([a].[PayloadSchemaJson]) = 1 THEN [a].[PayloadSchemaJson] ELSE N'{}' END,
                        '$.request.key'))), '')) > 100
                        OR LEN(NULLIF(LTRIM(RTRIM(JSON_VALUE(
                            CASE WHEN ISJSON([a].[PayloadSchemaJson]) = 1 THEN [a].[PayloadSchemaJson] ELSE N'{}' END,
                            '$.key'))), '')) > 100
                )
                BEGIN
                    THROW 51006, 'Cannot migrate ContractArtifacts.ContractKey because at least one artifact schema key is longer than 100 characters.', 1;
                END;

                UPDATE [a]
                SET [ContractKey] = COALESCE(
                    NULLIF(LTRIM(RTRIM([e].[Key])), ''),
                    NULLIF(LTRIM(RTRIM([c].[Key])), ''),
                    NULLIF(LTRIM(RTRIM(JSON_VALUE(
                        CASE WHEN ISJSON([a].[PayloadSchemaJson]) = 1 THEN [a].[PayloadSchemaJson] ELSE N'{}' END,
                        '$.request.key'))), ''),
                    NULLIF(LTRIM(RTRIM(JSON_VALUE(
                        CASE WHEN ISJSON([a].[PayloadSchemaJson]) = 1 THEN [a].[PayloadSchemaJson] ELSE N'{}' END,
                        '$.key'))), ''))
                FROM [ContractArtifacts] AS [a]
                LEFT JOIN [Events] AS [e] ON [a].[ArtifactType] = N'Event' AND [a].[DefinitionId] = [e].[Id]
                LEFT JOIN [Commands] AS [c] ON [a].[ArtifactType] IN (N'Command', N'CommandRequest', N'CommandReply') AND [a].[DefinitionId] = [c].[Id];

                IF EXISTS (
                    SELECT 1
                    FROM [ContractArtifacts]
                    WHERE NULLIF(LTRIM(RTRIM([ContractKey])), '') IS NULL
                )
                BEGIN
                    THROW 51007, 'Cannot migrate ContractArtifacts.ContractKey because at least one artifact cannot be matched to a contract schema key.', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM [ContractArtifacts]
                    GROUP BY [ArtifactType], [ContractKey], [VersionNumber]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51008, 'Cannot migrate ContractArtifacts.ContractKey because the new artifact identity would create duplicates.', 1;
                END;
                """);

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

                IF EXISTS (
                    SELECT 1
                    FROM [RuntimeContractArtifacts]
                    GROUP BY [ArtifactType], [ContractKey], [VersionNumber]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51009, 'Cannot migrate RuntimeContractArtifacts.ContractKey because the new artifact identity would create duplicates.', 1;
                END;
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

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContractKey",
                table: "ContractArtifacts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Commands",
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

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "ContractArtifacts");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "Commands");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_ContractKey_VersionNumber",
                table: "RuntimeContractArtifacts",
                columns: new[] { "ArtifactType", "ContractKey", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_Key",
                table: "Events",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_ContractArtifacts_ArtifactType_ContractKey_VersionNumber",
                table: "ContractArtifacts",
                columns: new[] { "ArtifactType", "ContractKey", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commands_Key",
                table: "Commands",
                column: "Key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_ContractKey_VersionNumber",
                table: "RuntimeContractArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_Events_Key",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_ContractArtifacts_ArtifactType_ContractKey_VersionNumber",
                table: "ContractArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_Commands_Key",
                table: "Commands");

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "RuntimeContractArtifacts",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "Events",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "ContractArtifacts",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "Commands",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [RuntimeContractArtifacts]
                SET [Topic] = LEFT(NULLIF(LTRIM(RTRIM([ContractKey])), ''), 70);

                UPDATE [RuntimeContractArtifacts]
                SET [Topic] = CONCAT('runtime-artifact-', LOWER(CONVERT(char(36), [DefinitionId])))
                WHERE [Topic] IS NULL;

                UPDATE [Events]
                SET [Topic] = LEFT(NULLIF(LTRIM(RTRIM([Key])), ''), 70);

                UPDATE [Events]
                SET [Topic] = CONCAT('event-', LOWER(CONVERT(char(36), [Id])))
                WHERE [Topic] IS NULL;

                UPDATE [ContractArtifacts]
                SET [Topic] = LEFT(NULLIF(LTRIM(RTRIM([ContractKey])), ''), 70);

                UPDATE [ContractArtifacts]
                SET [Topic] = CONCAT('artifact-', LOWER(CONVERT(char(36), [DefinitionId])))
                WHERE [Topic] IS NULL;

                UPDATE [Commands]
                SET [Topic] = LEFT(NULLIF(LTRIM(RTRIM([Key])), ''), 70);

                UPDATE [Commands]
                SET [Topic] = CONCAT('command-', LOWER(CONVERT(char(36), [Id])))
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

            migrationBuilder.AlterColumn<string>(
                name: "Topic",
                table: "Events",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(70)",
                oldMaxLength: 70,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Topic",
                table: "ContractArtifacts",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(70)",
                oldMaxLength: 70,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Topic",
                table: "Commands",
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

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ContractKey",
                table: "ContractArtifacts");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Commands");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeContractArtifacts_ArtifactType_Topic_VersionNumber",
                table: "RuntimeContractArtifacts",
                columns: new[] { "ArtifactType", "Topic", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_Topic",
                table: "Events",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_ContractArtifacts_ArtifactType_Topic_VersionNumber",
                table: "ContractArtifacts",
                columns: new[] { "ArtifactType", "Topic", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commands_Topic",
                table: "Commands",
                column: "Topic");
        }
    }
}
