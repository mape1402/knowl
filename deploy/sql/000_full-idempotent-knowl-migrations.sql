IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505213529_InitialEventsSqlServer'
)
BEGIN
    CREATE TABLE [Events] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Version] nvarchar(13) NOT NULL,
        [Topic] nvarchar(70) NOT NULL,
        [Description] nvarchar(max) NULL,
        [PayloadSchemaJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505213529_InitialEventsSqlServer'
)
BEGIN
    CREATE INDEX [IX_Events_CreatedAtUtc] ON [Events] ([CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505213529_InitialEventsSqlServer'
)
BEGIN
    CREATE INDEX [IX_Events_Topic_Version] ON [Events] ([Topic], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505213529_InitialEventsSqlServer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260505213529_InitialEventsSqlServer', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505223737_AddCommandsTable'
)
BEGIN
    CREATE TABLE [Commands] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Version] nvarchar(13) NOT NULL,
        [Topic] nvarchar(70) NOT NULL,
        [Description] nvarchar(max) NULL,
        [PayloadSchemaJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Commands] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505223737_AddCommandsTable'
)
BEGIN
    CREATE INDEX [IX_Commands_CreatedAtUtc] ON [Commands] ([CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505223737_AddCommandsTable'
)
BEGIN
    CREATE INDEX [IX_Commands_Topic_Version] ON [Commands] ([Topic], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505223737_AddCommandsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260505223737_AddCommandsTable', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    DROP INDEX [IX_Events_Topic_Version] ON [Events];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    DROP INDEX [IX_Commands_Topic_Version] ON [Commands];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Events]') AND [c].[name] = N'PayloadSchemaJson');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Events] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Events] DROP COLUMN [PayloadSchemaJson];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Events]') AND [c].[name] = N'Version');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Events] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Events] DROP COLUMN [Version];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Commands]') AND [c].[name] = N'PayloadSchemaJson');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Commands] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Commands] DROP COLUMN [PayloadSchemaJson];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Commands]') AND [c].[name] = N'Version');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Commands] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Commands] DROP COLUMN [Version];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    CREATE TABLE [CommandVersions] (
        [Id] uniqueidentifier NOT NULL,
        [CommandDefinitionId] uniqueidentifier NOT NULL,
        [VersionNumber] nvarchar(13) NOT NULL,
        [PayloadSchemaJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_CommandVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CommandVersions_Commands_CommandDefinitionId] FOREIGN KEY ([CommandDefinitionId]) REFERENCES [Commands] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    CREATE TABLE [EventVersions] (
        [Id] uniqueidentifier NOT NULL,
        [EventDefinitionId] uniqueidentifier NOT NULL,
        [VersionNumber] nvarchar(13) NOT NULL,
        [PayloadSchemaJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_EventVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventVersions_Events_EventDefinitionId] FOREIGN KEY ([EventDefinitionId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    CREATE INDEX [IX_Events_Topic] ON [Events] ([Topic]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    CREATE INDEX [IX_Commands_Topic] ON [Commands] ([Topic]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CommandVersions_CommandDefinitionId_VersionNumber] ON [CommandVersions] ([CommandDefinitionId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    CREATE INDEX [IX_CommandVersions_CreatedAtUtc] ON [CommandVersions] ([CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    CREATE INDEX [IX_EventVersions_CreatedAtUtc] ON [EventVersions] ([CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EventVersions_EventDefinitionId_VersionNumber] ON [EventVersions] ([EventDefinitionId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506003855_RefactorContractsVersioning'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506003855_RefactorContractsVersioning', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506051438_AddVersionCommentFields'
)
BEGIN
    ALTER TABLE [EventVersions] ADD [Comment] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506051438_AddVersionCommentFields'
)
BEGIN
    ALTER TABLE [CommandVersions] ADD [Comment] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506051438_AddVersionCommentFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506051438_AddVersionCommentFields', N'10.0.3');
END;

COMMIT;
GO

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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603002755_AddContractFieldMetadataDefinitions'
)
BEGIN
    CREATE TABLE [ContractFieldMetadataDefinitions] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DataType] nvarchar(20) NOT NULL,
        [AppliesToJson] nvarchar(max) NOT NULL,
        [IsRequired] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [ValidationJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ContractFieldMetadataDefinitions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603002755_AddContractFieldMetadataDefinitions'
)
BEGIN
    CREATE INDEX [IX_ContractFieldMetadataDefinitions_IsActive_SortOrder] ON [ContractFieldMetadataDefinitions] ([IsActive], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603002755_AddContractFieldMetadataDefinitions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContractFieldMetadataDefinitions_Key] ON [ContractFieldMetadataDefinitions] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603002755_AddContractFieldMetadataDefinitions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603002755_AddContractFieldMetadataDefinitions', N'10.0.3');
END;

COMMIT;
GO

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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603192440_RemoveNullSchemaType'
)
BEGIN
    EXEC(N'DELETE FROM [SchemaTypeVersions]
    WHERE [Id] = ''21111111-1111-1111-1111-111111111117'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603192440_RemoveNullSchemaType'
)
BEGIN
    EXEC(N'DELETE FROM [SchemaTypeDefinitions]
    WHERE [Id] = ''11111111-1111-1111-1111-111111111117'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603192440_RemoveNullSchemaType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603192440_RemoveNullSchemaType', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    DROP INDEX [IX_ContractFieldMetadataDefinitions_IsActive_SortOrder] ON [ContractFieldMetadataDefinitions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    CREATE TABLE [ContractFieldMetadataVersions] (
        [Id] uniqueidentifier NOT NULL,
        [ContractFieldMetadataDefinitionId] uniqueidentifier NOT NULL,
        [VersionNumber] nvarchar(13) NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [DefinitionJson] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ContractFieldMetadataVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractFieldMetadataVersions_ContractFieldMetadataDefinitions_ContractFieldMetadataDefinitionId] FOREIGN KEY ([ContractFieldMetadataDefinitionId]) REFERENCES [ContractFieldMetadataDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    CREATE INDEX [IX_ContractFieldMetadataDefinitions_IsActive] ON [ContractFieldMetadataDefinitions] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContractFieldMetadataVersions_ContractFieldMetadataDefinitionId_VersionNumber] ON [ContractFieldMetadataVersions] ([ContractFieldMetadataDefinitionId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    CREATE INDEX [IX_ContractFieldMetadataVersions_IsActive] ON [ContractFieldMetadataVersions] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContractFieldMetadataDefinitions]') AND [c].[name] = N'AppliesToJson');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [ContractFieldMetadataDefinitions] DROP COLUMN [AppliesToJson];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContractFieldMetadataDefinitions]') AND [c].[name] = N'DataType');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [ContractFieldMetadataDefinitions] DROP COLUMN [DataType];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContractFieldMetadataDefinitions]') AND [c].[name] = N'IsRequired');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [ContractFieldMetadataDefinitions] DROP COLUMN [IsRequired];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContractFieldMetadataDefinitions]') AND [c].[name] = N'SortOrder');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [ContractFieldMetadataDefinitions] DROP COLUMN [SortOrder];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContractFieldMetadataDefinitions]') AND [c].[name] = N'ValidationJson');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [ContractFieldMetadataDefinitions] DROP COLUMN [ValidationJson];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701163956_VersionedContractFieldMetadata', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    DROP INDEX [IX_SchemaTypeVersions_BaseType_IsActive] ON [SchemaTypeVersions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    DROP INDEX [IX_SchemaTypeDefinitions_Name] ON [SchemaTypeDefinitions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    ALTER TABLE [SchemaTypeDefinitions] ADD [Key] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    ALTER TABLE [SchemaTypeVersions] ADD [DefinitionJson] nvarchar(max) NOT NULL DEFAULT N'{}';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    UPDATE [SchemaTypeDefinitions]
    SET [Key] = [Name]
    WHERE [Key] = N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SchemaTypeVersions]') AND [c].[name] = N'BaseType');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [SchemaTypeVersions] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [SchemaTypeVersions] DROP COLUMN [BaseType];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SchemaTypeVersions]') AND [c].[name] = N'JsonSchema');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [SchemaTypeVersions] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [SchemaTypeVersions] DROP COLUMN [JsonSchema];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    CREATE INDEX [IX_SchemaTypeVersions_IsActive] ON [SchemaTypeVersions] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SchemaTypeDefinitions_Key] ON [SchemaTypeDefinitions] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713190532_VersionedSchemaTypeDefinitions', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713193500_MigrateEventPayloadSchemasToButterMorphDefinitions'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713193500_MigrateEventPayloadSchemasToButterMorphDefinitions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713193500_MigrateEventPayloadSchemasToButterMorphDefinitions', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713204000_MigrateCommandPayloadSchemasToButterMorphDefinitions'
)
BEGIN
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
    FROM [CommandVersions] version_row
    INNER JOIN [Commands] definition
        ON definition.[Id] = version_row.[CommandDefinitionId]
    WHERE ISJSON(version_row.[PayloadSchemaJson]) = 1
      AND JSON_VALUE(version_row.[PayloadSchemaJson], '$.key') IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713204000_MigrateCommandPayloadSchemasToButterMorphDefinitions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713204000_MigrateCommandPayloadSchemasToButterMorphDefinitions', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [EventVersions] ADD [ApprovedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [EventVersions] ADD [ArchivedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [EventVersions] ADD [DeployedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [EventVersions] ADD [DeprecatedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [EventVersions] ADD [InReviewAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [EventVersions] ADD [Status] nvarchar(32) NOT NULL DEFAULT N'Draft';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [Events] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [CommandVersions] ADD [ApprovedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [CommandVersions] ADD [ArchivedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [CommandVersions] ADD [DeployedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [CommandVersions] ADD [DeprecatedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [CommandVersions] ADD [InReviewAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [CommandVersions] ADD [Status] nvarchar(32) NOT NULL DEFAULT N'Draft';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    ALTER TABLE [Commands] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    CREATE INDEX [IX_EventVersions_Status] ON [EventVersions] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    CREATE INDEX [IX_Events_IsActive] ON [Events] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    CREATE INDEX [IX_CommandVersions_Status] ON [CommandVersions] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    CREATE INDEX [IX_Commands_IsActive] ON [Commands] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725195512_AddContractLifecycleState'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725195512_AddContractLifecycleState', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200621_AddContractArtifacts'
)
BEGIN
    CREATE TABLE [ContractArtifacts] (
        [Id] uniqueidentifier NOT NULL,
        [ArtifactType] nvarchar(32) NOT NULL,
        [DefinitionId] uniqueidentifier NOT NULL,
        [VersionId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Topic] nvarchar(70) NOT NULL,
        [VersionNumber] nvarchar(13) NOT NULL,
        [Description] nvarchar(max) NULL,
        [PayloadSchemaJson] nvarchar(max) NOT NULL,
        [ContentHash] nvarchar(128) NOT NULL,
        [SourceStatus] nvarchar(32) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ContractArtifacts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200621_AddContractArtifacts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContractArtifacts_ArtifactType_Topic_VersionNumber] ON [ContractArtifacts] ([ArtifactType], [Topic], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200621_AddContractArtifacts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContractArtifacts_ArtifactType_VersionId] ON [ContractArtifacts] ([ArtifactType], [VersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200621_AddContractArtifacts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725200621_AddContractArtifacts', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725201049_AddContractReleases'
)
BEGIN
    CREATE TABLE [ContractReleases] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Status] nvarchar(32) NOT NULL DEFAULT N'Draft',
        [CreatedAtUtc] datetime2 NOT NULL,
        [InReviewAtUtc] datetime2 NULL,
        [ApprovedAtUtc] datetime2 NULL,
        [DeployedAtUtc] datetime2 NULL,
        [CanceledAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ContractReleases] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725201049_AddContractReleases'
)
BEGIN
    CREATE TABLE [ContractReleaseItems] (
        [Id] uniqueidentifier NOT NULL,
        [ReleaseId] uniqueidentifier NOT NULL,
        [ArtifactId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ContractReleaseItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractReleaseItems_ContractArtifacts_ArtifactId] FOREIGN KEY ([ArtifactId]) REFERENCES [ContractArtifacts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ContractReleaseItems_ContractReleases_ReleaseId] FOREIGN KEY ([ReleaseId]) REFERENCES [ContractReleases] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725201049_AddContractReleases'
)
BEGIN
    CREATE INDEX [IX_ContractReleaseItems_ArtifactId] ON [ContractReleaseItems] ([ArtifactId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725201049_AddContractReleases'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContractReleaseItems_ReleaseId_ArtifactId] ON [ContractReleaseItems] ([ReleaseId], [ArtifactId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725201049_AddContractReleases'
)
BEGIN
    CREATE INDEX [IX_ContractReleases_Status] ON [ContractReleases] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725201049_AddContractReleases'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725201049_AddContractReleases', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725201635_AddRuntimeContractArtifacts'
)
BEGIN
    CREATE TABLE [RuntimeContractArtifacts] (
        [Id] uniqueidentifier NOT NULL,
        [SourceArtifactId] uniqueidentifier NOT NULL,
        [SourceReleaseId] uniqueidentifier NOT NULL,
        [ArtifactType] nvarchar(32) NOT NULL,
        [DefinitionId] uniqueidentifier NOT NULL,
        [VersionId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Topic] nvarchar(70) NOT NULL,
        [VersionNumber] nvarchar(13) NOT NULL,
        [Description] nvarchar(max) NULL,
        [PayloadSchemaJson] nvarchar(max) NOT NULL,
        [ContentHash] nvarchar(128) NOT NULL,
        [DeployedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_RuntimeContractArtifacts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725201635_AddRuntimeContractArtifacts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuntimeContractArtifacts_ArtifactType_Topic_VersionNumber] ON [RuntimeContractArtifacts] ([ArtifactType], [Topic], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725201635_AddRuntimeContractArtifacts'
)
BEGIN
    CREATE INDEX [IX_RuntimeContractArtifacts_SourceReleaseId] ON [RuntimeContractArtifacts] ([SourceReleaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725201635_AddRuntimeContractArtifacts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725201635_AddRuntimeContractArtifacts', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    ALTER TABLE [ContractReleases] ADD [CompletedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    ALTER TABLE [ContractReleases] ADD [FailedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE TABLE [RuntimeNodes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [EnvironmentName] nvarchar(100) NOT NULL,
        [DistributionMode] nvarchar(32) NOT NULL,
        [EndpointBaseUri] nvarchar(500) NOT NULL,
        [EndpointApiPath] nvarchar(200) NOT NULL,
        [AuthenticationMode] nvarchar(32) NOT NULL,
        [ClientId] nvarchar(200) NOT NULL,
        [SecretReference] nvarchar(500) NOT NULL,
        [ApiKeyReference] nvarchar(500) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [IsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [Description] nvarchar(max) NULL,
        [RegisteredAtUtc] datetime2 NOT NULL,
        [LastUpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_RuntimeNodes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE TABLE [ContractReleaseTargets] (
        [Id] uniqueidentifier NOT NULL,
        [ReleaseId] uniqueidentifier NOT NULL,
        [ReleaseItemId] uniqueidentifier NOT NULL,
        [RuntimeNodeId] uniqueidentifier NOT NULL,
        [ArtifactId] uniqueidentifier NOT NULL,
        [RolloutGroup] nvarchar(100) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [ActivationStatus] nvarchar(32) NOT NULL,
        [AssignedAtUtc] datetime2 NOT NULL,
        [AvailableAtUtc] datetime2 NULL,
        [DeliveredAtUtc] datetime2 NULL,
        [AcknowledgedAtUtc] datetime2 NULL,
        [ActivatedAtUtc] datetime2 NULL,
        [FailedAtUtc] datetime2 NULL,
        [FailureReason] nvarchar(2000) NULL,
        [RuntimeVersionApplied] nvarchar(200) NOT NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_ContractReleaseTargets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractReleaseTargets_ContractArtifacts_ArtifactId] FOREIGN KEY ([ArtifactId]) REFERENCES [ContractArtifacts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ContractReleaseTargets_ContractReleaseItems_ReleaseItemId] FOREIGN KEY ([ReleaseItemId]) REFERENCES [ContractReleaseItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ContractReleaseTargets_ContractReleases_ReleaseId] FOREIGN KEY ([ReleaseId]) REFERENCES [ContractReleases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ContractReleaseTargets_RuntimeNodes_RuntimeNodeId] FOREIGN KEY ([RuntimeNodeId]) REFERENCES [RuntimeNodes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE INDEX [IX_ContractReleaseTargets_ArtifactId] ON [ContractReleaseTargets] ([ArtifactId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContractReleaseTargets_ReleaseId_ReleaseItemId_RuntimeNodeId] ON [ContractReleaseTargets] ([ReleaseId], [ReleaseItemId], [RuntimeNodeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE INDEX [IX_ContractReleaseTargets_ReleaseItemId] ON [ContractReleaseTargets] ([ReleaseItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE INDEX [IX_ContractReleaseTargets_RuntimeNodeId_Status] ON [ContractReleaseTargets] ([RuntimeNodeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuntimeNodes_Code] ON [RuntimeNodes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE INDEX [IX_RuntimeNodes_IsEnabled_Status] ON [RuntimeNodes] ([IsEnabled], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730015305_AddMultiRuntimeDistributionModel', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [EnvironmentId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    CREATE TABLE [RuntimeEnvironments] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_RuntimeEnvironments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    CREATE INDEX [IX_RuntimeNodes_EnvironmentId] ON [RuntimeNodes] ([EnvironmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuntimeEnvironments_Code] ON [RuntimeEnvironments] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    CREATE INDEX [IX_RuntimeEnvironments_IsEnabled] ON [RuntimeEnvironments] ([IsEnabled]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD CONSTRAINT [FK_RuntimeNodes_RuntimeEnvironments_EnvironmentId] FOREIGN KEY ([EnvironmentId]) REFERENCES [RuntimeEnvironments] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730033759_AddRuntimeEnvironments', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [AccessTokenTtlSeconds] int NOT NULL DEFAULT 86400;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [DeletedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundAllowedScopes] nvarchar(500) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundClientId] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundCredentialCreatedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundCredentialRevokedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundCredentialRotatedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundCredentialStatus] nvarchar(32) NOT NULL DEFAULT N'Missing';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundKeyId] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundLastFailureReason] nvarchar(2000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundLastTokenFailedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundLastTokenIssuedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundSecretHash] nvarchar(500) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundClientId] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundCredentialImportedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundCredentialStatus] nvarchar(32) NOT NULL DEFAULT N'Missing';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundKeyId] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundLastTokenReceivedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundRequestedScopes] nvarchar(500) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [ProtectedOutboundSecret] nvarchar(2000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [TokenRefreshSkewSeconds] int NOT NULL DEFAULT 300;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [TokenValidationCacheTtlSeconds] int NOT NULL DEFAULT 300;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    CREATE INDEX [IX_RuntimeNodes_InboundClientId] ON [RuntimeNodes] ([InboundClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260905191117_AddRuntimeNodeConnectionSecurity', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911003507_AddContractReleaseAttempts'
)
BEGIN
    CREATE TABLE [ContractReleaseAttempts] (
        [Id] uniqueidentifier NOT NULL,
        [ReleaseTargetId] uniqueidentifier NOT NULL,
        [Action] nvarchar(64) NOT NULL,
        [InitiatedBy] nvarchar(128) NOT NULL,
        [StartedAtUtc] datetime2 NOT NULL,
        [FinishedAtUtc] datetime2 NULL,
        [Succeeded] bit NOT NULL,
        [ErrorCode] nvarchar(128) NOT NULL,
        [ErrorMessage] nvarchar(2048) NOT NULL,
        [ExternalReference] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_ContractReleaseAttempts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractReleaseAttempts_ContractReleaseTargets_ReleaseTargetId] FOREIGN KEY ([ReleaseTargetId]) REFERENCES [ContractReleaseTargets] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911003507_AddContractReleaseAttempts'
)
BEGIN
    CREATE INDEX [IX_ContractReleaseAttempts_ReleaseTargetId_StartedAtUtc] ON [ContractReleaseAttempts] ([ReleaseTargetId], [StartedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911003507_AddContractReleaseAttempts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260911003507_AddContractReleaseAttempts', N'10.0.3');
END;

COMMIT;
GO

