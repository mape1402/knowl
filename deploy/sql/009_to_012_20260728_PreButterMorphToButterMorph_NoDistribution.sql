/*
    KnOwl SQL rollout: pre-ButterMorph -> ButterMorph, without Distribution/Runtime.

    Execution order:
      1. Run the existing KnOwl scripts up to 008 if they are not already applied.
      2. Run this script once.
      3. Do NOT run 013+ if the target scope excludes Distribution/Runtime.

    Included EF migrations:
      - 20260701163956_VersionedContractFieldMetadata
      - 20260713190532_VersionedSchemaTypeDefinitions
      - 20260713193500_MigrateEventPayloadSchemasToButterMorphDefinitions
      - 20260713204000_MigrateCommandPayloadSchemasToButterMorphDefinitions

    Data migration included:
      - ContractFieldMetadataDefinitions legacy columns are copied into ContractFieldMetadataVersions.DefinitionJson.
      - SchemaTypeVersions JsonSchema/BaseType are copied into SchemaTypeVersions.DefinitionJson.
      - EventVersions.PayloadSchemaJson legacy JSON is wrapped as a ButterMorph payload definition.
      - CommandVersions.PayloadSchemaJson legacy JSON is wrapped as a ButterMorph payload definition.
*/

IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    THROW 51000, 'KnOwl migration history table was not found. Apply base migrations before running this ButterMorph rollout script.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260603192440_RemoveNullSchemaType')
BEGIN
    THROW 51001, 'Expected KnOwl database state through migration 20260603192440_RemoveNullSchemaType before running the ButterMorph rollout script.', 1;
END;

IF EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] IN (
    N'20260725200621_AddContractArtifacts',
    N'20260725201049_AddContractReleases',
    N'20260725201635_AddRuntimeContractArtifacts'))
BEGIN
    PRINT 'Warning: Distribution/Runtime migrations are already present in this database. This script will only apply missing ButterMorph migrations 009-012.';
END;

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
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContractFieldMetadataDefinitions]') AND [c].[name] = N'AppliesToJson');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [ContractFieldMetadataDefinitions] DROP COLUMN [AppliesToJson];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContractFieldMetadataDefinitions]') AND [c].[name] = N'DataType');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [ContractFieldMetadataDefinitions] DROP COLUMN [DataType];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContractFieldMetadataDefinitions]') AND [c].[name] = N'IsRequired');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [ContractFieldMetadataDefinitions] DROP COLUMN [IsRequired];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContractFieldMetadataDefinitions]') AND [c].[name] = N'SortOrder');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [ContractFieldMetadataDefinitions] DROP COLUMN [SortOrder];
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
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContractFieldMetadataDefinitions]') AND [c].[name] = N'ValidationJson');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT ' + @var4 + ';');
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

