/*
    KnOwl SQL rollout v2: pre-ButterMorph -> ButterMorph, without Distribution/Runtime.

    Start state required:
      - __EFMigrationsHistory contains 20260603192440_RemoveNullSchemaType.

    Included EF migrations:
      - 20260701163956_VersionedContractFieldMetadata
      - 20260713190532_VersionedSchemaTypeDefinitions
      - 20260713193500_MigrateEventPayloadSchemasToButterMorphDefinitions
      - 20260713204000_MigrateCommandPayloadSchemasToButterMorphDefinitions

    Why v2 exists:
      - v1 used newly-created columns/tables in the same T-SQL batch.
      - SQL Server compiles the full batch before executing ALTER/CREATE statements, causing errors such as:
          Invalid column name 'DefinitionJson'.
          Invalid column name 'Key'.
      - v2 uses EXEC(...) for DDL/DML that references columns or tables created by the same migration.

    Safe re-run behavior:
      - Each migration checks __EFMigrationsHistory.
      - DDL also checks table/column/index existence where useful to recover from failed partial attempts.
*/

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    THROW 51000, 'KnOwl migration history table was not found. Apply base migrations before running this ButterMorph rollout script.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260603192440_RemoveNullSchemaType')
BEGIN
    THROW 51001, 'Expected KnOwl database state through migration 20260603192440_RemoveNullSchemaType before running the ButterMorph rollout script.', 1;
END;
GO

/* 009 - VersionedContractFieldMetadata */
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260701163956_VersionedContractFieldMetadata')
BEGIN
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContractFieldMetadataDefinitions_IsActive_SortOrder' AND object_id = OBJECT_ID(N'[ContractFieldMetadataDefinitions]'))
    BEGIN
        DROP INDEX [IX_ContractFieldMetadataDefinitions_IsActive_SortOrder] ON [ContractFieldMetadataDefinitions];
    END;

    IF OBJECT_ID(N'[ContractFieldMetadataVersions]', N'U') IS NULL
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

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContractFieldMetadataDefinitions_IsActive' AND object_id = OBJECT_ID(N'[ContractFieldMetadataDefinitions]'))
    BEGIN
        CREATE INDEX [IX_ContractFieldMetadataDefinitions_IsActive] ON [ContractFieldMetadataDefinitions] ([IsActive]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContractFieldMetadataVersions_ContractFieldMetadataDefinitionId_VersionNumber' AND object_id = OBJECT_ID(N'[ContractFieldMetadataVersions]'))
    BEGIN
        EXEC(N'CREATE UNIQUE INDEX [IX_ContractFieldMetadataVersions_ContractFieldMetadataDefinitionId_VersionNumber] ON [ContractFieldMetadataVersions] ([ContractFieldMetadataDefinitionId], [VersionNumber]);');
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContractFieldMetadataVersions_IsActive' AND object_id = OBJECT_ID(N'[ContractFieldMetadataVersions]'))
    BEGIN
        EXEC(N'CREATE INDEX [IX_ContractFieldMetadataVersions_IsActive] ON [ContractFieldMetadataVersions] ([IsActive]);');
    END;

    IF COL_LENGTH(N'ContractFieldMetadataDefinitions', N'Key') IS NULL
       OR COL_LENGTH(N'ContractFieldMetadataDefinitions', N'DataType') IS NULL
       OR COL_LENGTH(N'ContractFieldMetadataDefinitions', N'ValidationJson') IS NULL
    BEGIN
        THROW 51002, 'ContractFieldMetadataDefinitions does not match the expected legacy schema. Missing Key, DataType, or ValidationJson.', 1;
    END;

    EXEC(N'
        INSERT INTO [ContractFieldMetadataVersions]
            ([Id], [ContractFieldMetadataDefinitionId], [VersionNumber], [Comment], [DefinitionJson], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
        SELECT
            NEWID(),
            definition.[Id],
            N''1.0.0'',
            NULL,
            (
                SELECT
                    definition.[Key] AS [key],
                    definition.[Name] AS [name],
                    COALESCE(definition.[Description], N'''') AS [description],
                    N''1.0.0'' AS [version],
                    N'''' AS [versionComment],
                    definition.[DataType] AS [dataType],
                    JSON_QUERY(N''["Field"]'') AS [appliesTo],
                    definition.[IsRequired] AS [isRequired],
                    definition.[IsActive] AS [isActive],
                    CASE
                        WHEN ISJSON(definition.[ValidationJson]) = 1 THEN JSON_QUERY(definition.[ValidationJson])
                        ELSE JSON_QUERY(N''{}'')
                    END AS [validation]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            definition.[IsActive],
            definition.[CreatedAtUtc],
            definition.[UpdatedAtUtc]
        FROM [ContractFieldMetadataDefinitions] definition
        WHERE NOT EXISTS (
            SELECT 1
            FROM [ContractFieldMetadataVersions] version_row
            WHERE version_row.[ContractFieldMetadataDefinitionId] = definition.[Id]
              AND version_row.[VersionNumber] = N''1.0.0''
        );');

    DECLARE @metadataDropSql nvarchar(max) = N'';
    SELECT @metadataDropSql = @metadataDropSql + N'
        DECLARE @dc_' + REPLACE([c].[name], N' ', N'_') + N' nvarchar(max);
        SELECT @dc_' + REPLACE([c].[name], N' ', N'_') + N' = QUOTENAME([d].[name])
        FROM [sys].[default_constraints] [d]
        INNER JOIN [sys].[columns] [col] ON [d].[parent_column_id] = [col].[column_id] AND [d].[parent_object_id] = [col].[object_id]
        WHERE ([d].[parent_object_id] = OBJECT_ID(N''[ContractFieldMetadataDefinitions]'') AND [col].[name] = N''' + [c].[name] + N''');
        IF @dc_' + REPLACE([c].[name], N' ', N'_') + N' IS NOT NULL EXEC(N''ALTER TABLE [ContractFieldMetadataDefinitions] DROP CONSTRAINT '' + @dc_' + REPLACE([c].[name], N' ', N'_') + N' + N'';'');
        ALTER TABLE [ContractFieldMetadataDefinitions] DROP COLUMN [' + [c].[name] + N'];'
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID(N'[ContractFieldMetadataDefinitions]')
      AND c.name IN (N'AppliesToJson', N'DataType', N'IsRequired', N'SortOrder', N'ValidationJson');

    IF @metadataDropSql <> N''
    BEGIN
        EXEC(@metadataDropSql);
    END;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701163956_VersionedContractFieldMetadata', N'10.0.3');

    COMMIT;
END;
GO

/* 010 - VersionedSchemaTypeDefinitions */
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260713190532_VersionedSchemaTypeDefinitions')
BEGIN
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SchemaTypeVersions_BaseType_IsActive' AND object_id = OBJECT_ID(N'[SchemaTypeVersions]'))
    BEGIN
        DROP INDEX [IX_SchemaTypeVersions_BaseType_IsActive] ON [SchemaTypeVersions];
    END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SchemaTypeDefinitions_Name' AND object_id = OBJECT_ID(N'[SchemaTypeDefinitions]'))
    BEGIN
        DROP INDEX [IX_SchemaTypeDefinitions_Name] ON [SchemaTypeDefinitions];
    END;

    IF COL_LENGTH(N'SchemaTypeDefinitions', N'Key') IS NULL
    BEGIN
        ALTER TABLE [SchemaTypeDefinitions] ADD [Key] nvarchar(100) NOT NULL CONSTRAINT [DF_SchemaTypeDefinitions_Key_ButterMorph] DEFAULT N'';
    END;

    IF COL_LENGTH(N'SchemaTypeVersions', N'DefinitionJson') IS NULL
    BEGIN
        ALTER TABLE [SchemaTypeVersions] ADD [DefinitionJson] nvarchar(max) NOT NULL CONSTRAINT [DF_SchemaTypeVersions_DefinitionJson_ButterMorph] DEFAULT N'{}';
    END;

    IF COL_LENGTH(N'SchemaTypeVersions', N'BaseType') IS NULL OR COL_LENGTH(N'SchemaTypeVersions', N'JsonSchema') IS NULL
    BEGIN
        THROW 51003, 'SchemaTypeVersions does not match the expected legacy schema. Missing BaseType or JsonSchema.', 1;
    END;

    EXEC(N'
        UPDATE [SchemaTypeDefinitions]
        SET [Key] = [Name]
        WHERE [Key] = N'''';');

    EXEC(N'
        UPDATE version_row
        SET [DefinitionJson] = CONCAT(
            N''{"key":"'', STRING_ESCAPE(definition.[Key], ''json''),
            N''","name":"'', STRING_ESCAPE(definition.[Name], ''json''),
            N''","description":"'', STRING_ESCAPE(COALESCE(definition.[Description], N''''), ''json''),
            N''","version":"'', STRING_ESCAPE(version_row.[VersionNumber], ''json''),
            N''","baseType":"'', STRING_ESCAPE(version_row.[BaseType], ''json''),
            N''","comment":"'', STRING_ESCAPE(COALESCE(version_row.[Comment], N''''), ''json''),
            N''","schema":'', CASE WHEN ISJSON(version_row.[JsonSchema]) = 1 THEN version_row.[JsonSchema] ELSE N''{}'' END,
            N''}'')
        FROM [SchemaTypeVersions] version_row
        INNER JOIN [SchemaTypeDefinitions] definition
            ON definition.[Id] = version_row.[SchemaTypeDefinitionId]
        WHERE version_row.[DefinitionJson] = N''{}'' OR JSON_VALUE(version_row.[DefinitionJson], ''$.key'') IS NULL;');

    DECLARE @schemaDropSql nvarchar(max) = N'';
    SELECT @schemaDropSql = @schemaDropSql + N'
        DECLARE @dc_' + [c].[name] + N' nvarchar(max);
        SELECT @dc_' + [c].[name] + N' = QUOTENAME([d].[name])
        FROM [sys].[default_constraints] [d]
        INNER JOIN [sys].[columns] [col] ON [d].[parent_column_id] = [col].[column_id] AND [d].[parent_object_id] = [col].[object_id]
        WHERE ([d].[parent_object_id] = OBJECT_ID(N''[SchemaTypeVersions]'') AND [col].[name] = N''' + [c].[name] + N''');
        IF @dc_' + [c].[name] + N' IS NOT NULL EXEC(N''ALTER TABLE [SchemaTypeVersions] DROP CONSTRAINT '' + @dc_' + [c].[name] + N' + N'';'');
        ALTER TABLE [SchemaTypeVersions] DROP COLUMN [' + [c].[name] + N'];'
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID(N'[SchemaTypeVersions]')
      AND c.name IN (N'BaseType', N'JsonSchema');

    IF @schemaDropSql <> N''
    BEGIN
        EXEC(@schemaDropSql);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SchemaTypeVersions_IsActive' AND object_id = OBJECT_ID(N'[SchemaTypeVersions]'))
    BEGIN
        CREATE INDEX [IX_SchemaTypeVersions_IsActive] ON [SchemaTypeVersions] ([IsActive]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SchemaTypeDefinitions_Key' AND object_id = OBJECT_ID(N'[SchemaTypeDefinitions]'))
    BEGIN
        EXEC(N'CREATE UNIQUE INDEX [IX_SchemaTypeDefinitions_Key] ON [SchemaTypeDefinitions] ([Key]);');
    END;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713190532_VersionedSchemaTypeDefinitions', N'10.0.3');

    COMMIT;
END;
GO

/* 011 - MigrateEventPayloadSchemasToButterMorphDefinitions */
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260713193500_MigrateEventPayloadSchemasToButterMorphDefinitions')
BEGIN
    BEGIN TRANSACTION;

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

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713193500_MigrateEventPayloadSchemasToButterMorphDefinitions', N'10.0.3');

    COMMIT;
END;
GO

/* 012 - MigrateCommandPayloadSchemasToButterMorphDefinitions */
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260713204000_MigrateCommandPayloadSchemasToButterMorphDefinitions')
BEGIN
    BEGIN TRANSACTION;

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

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713204000_MigrateCommandPayloadSchemasToButterMorphDefinitions', N'10.0.3');

    COMMIT;
END;
GO

