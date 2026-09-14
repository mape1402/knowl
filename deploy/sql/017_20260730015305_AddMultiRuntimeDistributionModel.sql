BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    ALTER TABLE [ContractReleases] ADD [CompletedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    ALTER TABLE [ContractReleases] ADD [FailedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE TABLE [RuntimeNodes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [EnvironmentName] nvarchar(100) NOT NULL,
        [DistributionMode] nvarchar(32) NOT NULL,
        [EndpointBaseUri] nvarchar(500) NOT NULL,
        [EndpointApiPath] nvarchar(200) NOT NULL,
        [AuthenticationMode] nvarchar(32) NOT NULL,
        [ClientId] nvarchar(200) NOT NULL,
        [SecretReference] nvarchar(500) NOT NULL,
        [ApiKeyReference] nvarchar(500) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [IsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [Description] nvarchar(max) NULL,
        [RegisteredAtUtc] datetime2 NOT NULL,
        [LastUpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_RuntimeNodes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE TABLE [ContractReleaseTargets] (
        [Id] uniqueidentifier NOT NULL,
        [ReleaseId] uniqueidentifier NOT NULL,
        [ReleaseItemId] uniqueidentifier NOT NULL,
        [RuntimeNodeId] uniqueidentifier NOT NULL,
        [ArtifactId] uniqueidentifier NOT NULL,
        [RolloutGroup] nvarchar(100) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [ActivationStatus] nvarchar(32) NOT NULL,
        [AssignedAtUtc] datetime2 NOT NULL,
        [AvailableAtUtc] datetime2 NULL,
        [DeliveredAtUtc] datetime2 NULL,
        [AcknowledgedAtUtc] datetime2 NULL,
        [ActivatedAtUtc] datetime2 NULL,
        [FailedAtUtc] datetime2 NULL,
        [FailureReason] nvarchar(2000) NULL,
        [RuntimeVersionApplied] nvarchar(200) NOT NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_ContractReleaseTargets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractReleaseTargets_ContractArtifacts_ArtifactId] FOREIGN KEY ([ArtifactId]) REFERENCES [ContractArtifacts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ContractReleaseTargets_ContractReleaseItems_ReleaseItemId] FOREIGN KEY ([ReleaseItemId]) REFERENCES [ContractReleaseItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ContractReleaseTargets_ContractReleases_ReleaseId] FOREIGN KEY ([ReleaseId]) REFERENCES [ContractReleases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ContractReleaseTargets_RuntimeNodes_RuntimeNodeId] FOREIGN KEY ([RuntimeNodeId]) REFERENCES [RuntimeNodes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE INDEX [IX_ContractReleaseTargets_ArtifactId] ON [ContractReleaseTargets] ([ArtifactId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContractReleaseTargets_ReleaseId_ReleaseItemId_RuntimeNodeId] ON [ContractReleaseTargets] ([ReleaseId], [ReleaseItemId], [RuntimeNodeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE INDEX [IX_ContractReleaseTargets_ReleaseItemId] ON [ContractReleaseTargets] ([ReleaseItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE INDEX [IX_ContractReleaseTargets_RuntimeNodeId_Status] ON [ContractReleaseTargets] ([RuntimeNodeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuntimeNodes_Code] ON [RuntimeNodes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    CREATE INDEX [IX_RuntimeNodes_IsEnabled_Status] ON [RuntimeNodes] ([IsEnabled], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730015305_AddMultiRuntimeDistributionModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730015305_AddMultiRuntimeDistributionModel', N'10.0.3');
END;

COMMIT;
GO

