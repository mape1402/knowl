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

