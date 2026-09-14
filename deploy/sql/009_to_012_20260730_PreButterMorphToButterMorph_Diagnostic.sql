/*
    KnOwl ButterMorph rollout diagnostic. Read-only.
    Run this before v2 if production is in a weird partial state.
*/

SELECT [MigrationId], [ProductVersion]
FROM [__EFMigrationsHistory]
WHERE [MigrationId] IN (
    N'20260603192440_RemoveNullSchemaType',
    N'20260701163956_VersionedContractFieldMetadata',
    N'20260713190532_VersionedSchemaTypeDefinitions',
    N'20260713193500_MigrateEventPayloadSchemasToButterMorphDefinitions',
    N'20260713204000_MigrateCommandPayloadSchemasToButterMorphDefinitions'
)
ORDER BY [MigrationId];

SELECT
    N'ContractFieldMetadataDefinitions' AS [TableName],
    [name] AS [ColumnName]
FROM sys.columns
WHERE object_id = OBJECT_ID(N'[ContractFieldMetadataDefinitions]')
  AND [name] IN (N'Key', N'DataType', N'ValidationJson', N'AppliesToJson', N'IsRequired', N'SortOrder')
UNION ALL
SELECT
    N'ContractFieldMetadataVersions',
    [name]
FROM sys.columns
WHERE object_id = OBJECT_ID(N'[ContractFieldMetadataVersions]')
  AND [name] IN (N'DefinitionJson', N'VersionNumber', N'ContractFieldMetadataDefinitionId')
UNION ALL
SELECT
    N'SchemaTypeDefinitions',
    [name]
FROM sys.columns
WHERE object_id = OBJECT_ID(N'[SchemaTypeDefinitions]')
  AND [name] IN (N'Key', N'Name')
UNION ALL
SELECT
    N'SchemaTypeVersions',
    [name]
FROM sys.columns
WHERE object_id = OBJECT_ID(N'[SchemaTypeVersions]')
  AND [name] IN (N'DefinitionJson', N'BaseType', N'JsonSchema', N'VersionNumber')
ORDER BY [TableName], [ColumnName];
