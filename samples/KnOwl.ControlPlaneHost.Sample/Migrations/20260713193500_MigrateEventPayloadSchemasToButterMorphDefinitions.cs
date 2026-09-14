using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnOwl.Migrations
{
    /// <inheritdoc />
    public partial class MigrateEventPayloadSchemasToButterMorphDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE version_row
                SET [PayloadSchemaJson] = CONCAT(
                    N'{"key":"', STRING_ESCAPE(definition.[Name], 'json'),
                    N'","name":"', STRING_ESCAPE(definition.[Name], 'json'),
                    N'","description":"', STRING_ESCAPE(COALESCE(definition.[Description], N''), 'json'),
                    N'","version":"', STRING_ESCAPE(version_row.[VersionNumber], 'json'),
                    N'","versionComment":"', STRING_ESCAPE(COALESCE(version_row.[Comment], N''), 'json'),
                    N'","metadata":{"topic":{"type":"string","value":"', STRING_ESCAPE(definition.[Topic], 'json'), N'"}},',
                    N'"type":"', STRING_ESCAPE(COALESCE(JSON_VALUE(version_row.[PayloadSchemaJson], '$.type'), N'object'), 'json'), N'",',
                    N'"properties":', COALESCE(JSON_QUERY(version_row.[PayloadSchemaJson], '$.properties'), N'{}'), N',',
                    N'"$defs":', COALESCE(JSON_QUERY(version_row.[PayloadSchemaJson], '$."$defs"'), N'{}'), N',',
                    N'"$metadataDefs":', COALESCE(JSON_QUERY(version_row.[PayloadSchemaJson], '$."$metadataDefs"'), N'{}'),
                    N'}')
                FROM [EventVersions] version_row
                INNER JOIN [Events] definition
                    ON definition.[Id] = version_row.[EventDefinitionId]
                WHERE ISJSON(version_row.[PayloadSchemaJson]) = 1
                  AND JSON_VALUE(version_row.[PayloadSchemaJson], '$.key') IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [EventVersions]
                SET [PayloadSchemaJson] = CONCAT(
                    N'{"type":"', STRING_ESCAPE(COALESCE(JSON_VALUE([PayloadSchemaJson], '$.type'), N'object'), 'json'), N'",',
                    N'"properties":', COALESCE(JSON_QUERY([PayloadSchemaJson], '$.properties'), N'{}'), N',',
                    N'"$defs":', COALESCE(JSON_QUERY([PayloadSchemaJson], '$."$defs"'), N'{}'), N'}')
                WHERE ISJSON([PayloadSchemaJson]) = 1
                  AND JSON_VALUE([PayloadSchemaJson], '$.key') IS NOT NULL;
                """);
        }
    }
}

