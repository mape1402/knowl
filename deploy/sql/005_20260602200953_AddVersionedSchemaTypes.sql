BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602200953_AddVersionedSchemaTypes'
)
BEGIN
    CREATE TABLE [SchemaTypeDefinitions] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsSystem] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_SchemaTypeDefinitions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602200953_AddVersionedSchemaTypes'
)
BEGIN
    CREATE TABLE [SchemaTypeVersions] (
        [Id] uniqueidentifier NOT NULL,
        [SchemaTypeDefinitionId] uniqueidentifier NOT NULL,
        [VersionNumber] nvarchar(13) NOT NULL,
        [BaseType] nvarchar(20) NOT NULL,
        [JsonSchema] nvarchar(max) NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_SchemaTypeVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SchemaTypeVersions_SchemaTypeDefinitions_SchemaTypeDefinitionId] FOREIGN KEY ([SchemaTypeDefinitionId]) REFERENCES [SchemaTypeDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602200953_AddVersionedSchemaTypes'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'Description', N'IsActive', N'IsSystem', N'Name', N'UpdatedAtUtc') AND [object_id] = OBJECT_ID(N'[SchemaTypeDefinitions]'))
        SET IDENTITY_INSERT [SchemaTypeDefinitions] ON;
    EXEC(N'INSERT INTO [SchemaTypeDefinitions] ([Id], [CreatedAtUtc], [Description], [IsActive], [IsSystem], [Name], [UpdatedAtUtc])
    VALUES (''11111111-1111-1111-1111-111111111111'', ''2026-06-02T00:00:00.0000000Z'', N''JSON Schema basic type: string'', CAST(1 AS bit), CAST(1 AS bit), N''string'', ''2026-06-02T00:00:00.0000000Z''),
    (''11111111-1111-1111-1111-111111111112'', ''2026-06-02T00:00:00.0000000Z'', N''JSON Schema basic type: number'', CAST(1 AS bit), CAST(1 AS bit), N''number'', ''2026-06-02T00:00:00.0000000Z''),
    (''11111111-1111-1111-1111-111111111113'', ''2026-06-02T00:00:00.0000000Z'', N''JSON Schema basic type: integer'', CAST(1 AS bit), CAST(1 AS bit), N''integer'', ''2026-06-02T00:00:00.0000000Z''),
    (''11111111-1111-1111-1111-111111111114'', ''2026-06-02T00:00:00.0000000Z'', N''JSON Schema basic type: boolean'', CAST(1 AS bit), CAST(1 AS bit), N''boolean'', ''2026-06-02T00:00:00.0000000Z''),
    (''11111111-1111-1111-1111-111111111115'', ''2026-06-02T00:00:00.0000000Z'', N''JSON Schema basic type: object'', CAST(1 AS bit), CAST(1 AS bit), N''object'', ''2026-06-02T00:00:00.0000000Z''),
    (''11111111-1111-1111-1111-111111111116'', ''2026-06-02T00:00:00.0000000Z'', N''JSON Schema basic type: array'', CAST(1 AS bit), CAST(1 AS bit), N''array'', ''2026-06-02T00:00:00.0000000Z''),
    (''11111111-1111-1111-1111-111111111117'', ''2026-06-02T00:00:00.0000000Z'', N''JSON Schema basic type: null'', CAST(1 AS bit), CAST(1 AS bit), N''null'', ''2026-06-02T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'Description', N'IsActive', N'IsSystem', N'Name', N'UpdatedAtUtc') AND [object_id] = OBJECT_ID(N'[SchemaTypeDefinitions]'))
        SET IDENTITY_INSERT [SchemaTypeDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602200953_AddVersionedSchemaTypes'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseType', N'Comment', N'CreatedAtUtc', N'IsActive', N'JsonSchema', N'SchemaTypeDefinitionId', N'UpdatedAtUtc', N'VersionNumber') AND [object_id] = OBJECT_ID(N'[SchemaTypeVersions]'))
        SET IDENTITY_INSERT [SchemaTypeVersions] ON;
    EXEC(N'INSERT INTO [SchemaTypeVersions] ([Id], [BaseType], [Comment], [CreatedAtUtc], [IsActive], [JsonSchema], [SchemaTypeDefinitionId], [UpdatedAtUtc], [VersionNumber])
    VALUES (''21111111-1111-1111-1111-111111111111'', N''string'', NULL, ''2026-06-02T00:00:00.0000000Z'', CAST(1 AS bit), N''{"type":"string"}'', ''11111111-1111-1111-1111-111111111111'', ''2026-06-02T00:00:00.0000000Z'', N''1.0.0''),
    (''21111111-1111-1111-1111-111111111112'', N''number'', NULL, ''2026-06-02T00:00:00.0000000Z'', CAST(1 AS bit), N''{"type":"number"}'', ''11111111-1111-1111-1111-111111111112'', ''2026-06-02T00:00:00.0000000Z'', N''1.0.0''),
    (''21111111-1111-1111-1111-111111111113'', N''integer'', NULL, ''2026-06-02T00:00:00.0000000Z'', CAST(1 AS bit), N''{"type":"integer"}'', ''11111111-1111-1111-1111-111111111113'', ''2026-06-02T00:00:00.0000000Z'', N''1.0.0''),
    (''21111111-1111-1111-1111-111111111114'', N''boolean'', NULL, ''2026-06-02T00:00:00.0000000Z'', CAST(1 AS bit), N''{"type":"boolean"}'', ''11111111-1111-1111-1111-111111111114'', ''2026-06-02T00:00:00.0000000Z'', N''1.0.0''),
    (''21111111-1111-1111-1111-111111111115'', N''object'', NULL, ''2026-06-02T00:00:00.0000000Z'', CAST(1 AS bit), N''{"type":"object"}'', ''11111111-1111-1111-1111-111111111115'', ''2026-06-02T00:00:00.0000000Z'', N''1.0.0''),
    (''21111111-1111-1111-1111-111111111116'', N''array'', NULL, ''2026-06-02T00:00:00.0000000Z'', CAST(1 AS bit), N''{"type":"array"}'', ''11111111-1111-1111-1111-111111111116'', ''2026-06-02T00:00:00.0000000Z'', N''1.0.0''),
    (''21111111-1111-1111-1111-111111111117'', N''null'', NULL, ''2026-06-02T00:00:00.0000000Z'', CAST(1 AS bit), N''{"type":"null"}'', ''11111111-1111-1111-1111-111111111117'', ''2026-06-02T00:00:00.0000000Z'', N''1.0.0'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseType', N'Comment', N'CreatedAtUtc', N'IsActive', N'JsonSchema', N'SchemaTypeDefinitionId', N'UpdatedAtUtc', N'VersionNumber') AND [object_id] = OBJECT_ID(N'[SchemaTypeVersions]'))
        SET IDENTITY_INSERT [SchemaTypeVersions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602200953_AddVersionedSchemaTypes'
)
BEGIN
    CREATE INDEX [IX_SchemaTypeDefinitions_IsActive] ON [SchemaTypeDefinitions] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602200953_AddVersionedSchemaTypes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SchemaTypeDefinitions_Name] ON [SchemaTypeDefinitions] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602200953_AddVersionedSchemaTypes'
)
BEGIN
    CREATE INDEX [IX_SchemaTypeVersions_BaseType_IsActive] ON [SchemaTypeVersions] ([BaseType], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602200953_AddVersionedSchemaTypes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SchemaTypeVersions_SchemaTypeDefinitionId_VersionNumber] ON [SchemaTypeVersions] ([SchemaTypeDefinitionId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602200953_AddVersionedSchemaTypes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260602200953_AddVersionedSchemaTypes', N'10.0.3');
END;

COMMIT;
GO

