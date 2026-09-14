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

