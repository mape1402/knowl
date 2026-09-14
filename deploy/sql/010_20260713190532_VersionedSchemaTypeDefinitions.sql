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
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SchemaTypeVersions]') AND [c].[name] = N'BaseType');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [SchemaTypeVersions] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [SchemaTypeVersions] DROP COLUMN [BaseType];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SchemaTypeVersions]') AND [c].[name] = N'JsonSchema');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [SchemaTypeVersions] DROP CONSTRAINT ' + @var1 + ';');
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

