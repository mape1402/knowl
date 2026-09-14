/*
    KnOwl repair script: fixes a partially-applied 20260713190532_VersionedSchemaTypeDefinitions migration.

    Symptom:
      __EFMigrationsHistory contains 20260713190532_VersionedSchemaTypeDefinitions,
      but physical columns are inconsistent:
        - Missing SchemaTypeDefinitions.Key
        - Missing SchemaTypeVersions.DefinitionJson
        - Still has SchemaTypeVersions.BaseType / JsonSchema

    This script does not insert/delete migration history. It repairs the physical schema to match the current KnOwl model.
*/

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'[SchemaTypeDefinitions]', N'U') IS NULL
BEGIN
    THROW 51100, 'SchemaTypeDefinitions table was not found.', 1;
END;
GO

IF OBJECT_ID(N'[SchemaTypeVersions]', N'U') IS NULL
BEGIN
    THROW 51101, 'SchemaTypeVersions table was not found.', 1;
END;
GO

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
    ALTER TABLE [SchemaTypeDefinitions]
        ADD [Key] nvarchar(100) NOT NULL CONSTRAINT [DF_SchemaTypeDefinitions_Key_Repair] DEFAULT N'';
END;

IF COL_LENGTH(N'SchemaTypeVersions', N'DefinitionJson') IS NULL
BEGIN
    ALTER TABLE [SchemaTypeVersions]
        ADD [DefinitionJson] nvarchar(max) NOT NULL CONSTRAINT [DF_SchemaTypeVersions_DefinitionJson_Repair] DEFAULT N'{}';
END;

EXEC(N'
    UPDATE [SchemaTypeDefinitions]
    SET [Key] = [Name]
    WHERE [Key] IS NULL OR [Key] = N'''';
');

IF COL_LENGTH(N'SchemaTypeVersions', N'BaseType') IS NOT NULL
   AND COL_LENGTH(N'SchemaTypeVersions', N'JsonSchema') IS NOT NULL
BEGIN
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
        WHERE version_row.[DefinitionJson] = N''{}'' OR JSON_VALUE(version_row.[DefinitionJson], ''$.key'') IS NULL;
    ');
END;

DECLARE @dropLegacyColumnsSql nvarchar(max) = N'';
SELECT @dropLegacyColumnsSql = @dropLegacyColumnsSql + N'
    DECLARE @dc_' + [c].[name] + N' nvarchar(max);
    SELECT @dc_' + [c].[name] + N' = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [col]
        ON [d].[parent_column_id] = [col].[column_id]
       AND [d].[parent_object_id] = [col].[object_id]
    WHERE [d].[parent_object_id] = OBJECT_ID(N''[SchemaTypeVersions]'')
      AND [col].[name] = N''' + [c].[name] + N''';

    IF @dc_' + [c].[name] + N' IS NOT NULL
        EXEC(N''ALTER TABLE [SchemaTypeVersions] DROP CONSTRAINT '' + @dc_' + [c].[name] + N' + N'';'');

    ALTER TABLE [SchemaTypeVersions] DROP COLUMN [' + [c].[name] + N'];
'
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'[SchemaTypeVersions]')
  AND c.name IN (N'BaseType', N'JsonSchema');

IF @dropLegacyColumnsSql <> N''
BEGIN
    EXEC(@dropLegacyColumnsSql);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SchemaTypeVersions_IsActive' AND object_id = OBJECT_ID(N'[SchemaTypeVersions]'))
BEGIN
    CREATE INDEX [IX_SchemaTypeVersions_IsActive] ON [SchemaTypeVersions] ([IsActive]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SchemaTypeDefinitions_Key' AND object_id = OBJECT_ID(N'[SchemaTypeDefinitions]'))
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SchemaTypeDefinitions_Key] ON [SchemaTypeDefinitions] ([Key]);');
END;

COMMIT;
GO

SELECT TABLE_NAME, COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN (N'SchemaTypeDefinitions', N'SchemaTypeVersions')
  AND COLUMN_NAME IN (N'Key', N'DefinitionJson', N'BaseType', N'JsonSchema')
ORDER BY TABLE_NAME, COLUMN_NAME;
GO
