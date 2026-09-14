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

