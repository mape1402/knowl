BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [EnvironmentId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    CREATE TABLE [RuntimeEnvironments] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_RuntimeEnvironments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    CREATE INDEX [IX_RuntimeNodes_EnvironmentId] ON [RuntimeNodes] ([EnvironmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuntimeEnvironments_Code] ON [RuntimeEnvironments] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    CREATE INDEX [IX_RuntimeEnvironments_IsEnabled] ON [RuntimeEnvironments] ([IsEnabled]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD CONSTRAINT [FK_RuntimeNodes_RuntimeEnvironments_EnvironmentId] FOREIGN KEY ([EnvironmentId]) REFERENCES [RuntimeEnvironments] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730033759_AddRuntimeEnvironments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730033759_AddRuntimeEnvironments', N'10.0.3');
END;

COMMIT;
GO

