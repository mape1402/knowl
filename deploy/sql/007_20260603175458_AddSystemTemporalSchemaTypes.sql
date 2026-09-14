BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603175458_AddSystemTemporalSchemaTypes'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'Description', N'IsActive', N'IsSystem', N'Name', N'UpdatedAtUtc') AND [object_id] = OBJECT_ID(N'[SchemaTypeDefinitions]'))
        SET IDENTITY_INSERT [SchemaTypeDefinitions] ON;
    EXEC(N'INSERT INTO [SchemaTypeDefinitions] ([Id], [CreatedAtUtc], [Description], [IsActive], [IsSystem], [Name], [UpdatedAtUtc])
    VALUES (''11111111-1111-1111-1111-111111111118'', ''2026-06-02T00:00:00.0000000Z'', N''System schema type: Date'', CAST(1 AS bit), CAST(1 AS bit), N''Date'', ''2026-06-02T00:00:00.0000000Z''),
    (''11111111-1111-1111-1111-111111111119'', ''2026-06-02T00:00:00.0000000Z'', N''System schema type: DateTime'', CAST(1 AS bit), CAST(1 AS bit), N''DateTime'', ''2026-06-02T00:00:00.0000000Z''),
    (''11111111-1111-1111-1111-111111111120'', ''2026-06-02T00:00:00.0000000Z'', N''System schema type: TimeSpan'', CAST(1 AS bit), CAST(1 AS bit), N''TimeSpan'', ''2026-06-02T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'Description', N'IsActive', N'IsSystem', N'Name', N'UpdatedAtUtc') AND [object_id] = OBJECT_ID(N'[SchemaTypeDefinitions]'))
        SET IDENTITY_INSERT [SchemaTypeDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603175458_AddSystemTemporalSchemaTypes'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseType', N'Comment', N'CreatedAtUtc', N'IsActive', N'JsonSchema', N'SchemaTypeDefinitionId', N'UpdatedAtUtc', N'VersionNumber') AND [object_id] = OBJECT_ID(N'[SchemaTypeVersions]'))
        SET IDENTITY_INSERT [SchemaTypeVersions] ON;
    EXEC(N'INSERT INTO [SchemaTypeVersions] ([Id], [BaseType], [Comment], [CreatedAtUtc], [IsActive], [JsonSchema], [SchemaTypeDefinitionId], [UpdatedAtUtc], [VersionNumber])
    VALUES (''21111111-1111-1111-1111-111111111118'', N''string'', NULL, ''2026-06-02T00:00:00.0000000Z'', CAST(1 AS bit), N''{"type":"string","pattern":"^\\d{4}-\\d{2}-\\d{2}$"}'', ''11111111-1111-1111-1111-111111111118'', ''2026-06-02T00:00:00.0000000Z'', N''1.0.0''),
    (''21111111-1111-1111-1111-111111111119'', N''string'', NULL, ''2026-06-02T00:00:00.0000000Z'', CAST(1 AS bit), N''{"type":"string","pattern":"^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(?:\\.\\d{1,7})?(?:Z|[+-]\\d{2}:\\d{2})?$"}'', ''11111111-1111-1111-1111-111111111119'', ''2026-06-02T00:00:00.0000000Z'', N''1.0.0''),
    (''21111111-1111-1111-1111-111111111120'', N''string'', NULL, ''2026-06-02T00:00:00.0000000Z'', CAST(1 AS bit), N''{"type":"string","pattern":"^(?:\\d+\\.)?\\d{2}:\\d{2}:\\d{2}(?:\\.\\d{1,7})?$"}'', ''11111111-1111-1111-1111-111111111120'', ''2026-06-02T00:00:00.0000000Z'', N''1.0.0'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseType', N'Comment', N'CreatedAtUtc', N'IsActive', N'JsonSchema', N'SchemaTypeDefinitionId', N'UpdatedAtUtc', N'VersionNumber') AND [object_id] = OBJECT_ID(N'[SchemaTypeVersions]'))
        SET IDENTITY_INSERT [SchemaTypeVersions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603175458_AddSystemTemporalSchemaTypes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603175458_AddSystemTemporalSchemaTypes', N'10.0.3');
END;

COMMIT;
GO

