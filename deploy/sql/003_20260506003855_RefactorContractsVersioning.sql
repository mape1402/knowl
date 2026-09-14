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

