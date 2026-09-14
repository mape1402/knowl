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

