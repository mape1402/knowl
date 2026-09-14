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

