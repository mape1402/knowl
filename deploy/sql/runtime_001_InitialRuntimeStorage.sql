IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191225_InitialRuntimeStorage'
)
BEGIN
    CREATE TABLE [RuntimeContractArtifacts] (
        [Id] uniqueidentifier NOT NULL,
        [SourceArtifactId] uniqueidentifier NOT NULL,
        [SourceReleaseId] uniqueidentifier NOT NULL,
        [ArtifactType] nvarchar(32) NOT NULL,
        [DefinitionId] uniqueidentifier NOT NULL,
        [VersionId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Topic] nvarchar(70) NOT NULL,
        [VersionNumber] nvarchar(13) NOT NULL,
        [Description] nvarchar(max) NULL,
        [PayloadSchemaJson] nvarchar(max) NOT NULL,
        [ContentHash] nvarchar(128) NOT NULL,
        [DeployedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_RuntimeContractArtifacts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191225_InitialRuntimeStorage'
)
BEGIN
    CREATE TABLE [RuntimeDesignNodes] (
        [Id] uniqueidentifier NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [EndpointBaseUri] nvarchar(500) NOT NULL,
        [RemoteRuntimeNodeId] nvarchar(100) NOT NULL,
        [DistributionMode] nvarchar(32) NOT NULL,
        [AccessTokenTtlSeconds] int NOT NULL,
        [TokenRefreshSkewSeconds] int NOT NULL,
        [TokenValidationCacheTtlSeconds] int NOT NULL,
        [InboundClientId] nvarchar(200) NOT NULL,
        [InboundKeyId] nvarchar(200) NOT NULL,
        [InboundSecretHash] nvarchar(500) NOT NULL,
        [InboundAllowedScopes] nvarchar(500) NOT NULL,
        [InboundCredentialStatus] nvarchar(32) NOT NULL DEFAULT N'Missing',
        [InboundCredentialCreatedAtUtc] datetime2 NULL,
        [InboundCredentialRotatedAtUtc] datetime2 NULL,
        [InboundCredentialRevokedAtUtc] datetime2 NULL,
        [InboundLastTokenIssuedAtUtc] datetime2 NULL,
        [InboundLastTokenFailedAtUtc] datetime2 NULL,
        [InboundLastFailureReason] nvarchar(2000) NOT NULL,
        [OutboundClientId] nvarchar(200) NOT NULL,
        [OutboundKeyId] nvarchar(200) NOT NULL,
        [ProtectedOutboundSecret] nvarchar(2000) NOT NULL,
        [OutboundRequestedScopes] nvarchar(500) NOT NULL,
        [OutboundCredentialStatus] nvarchar(32) NOT NULL DEFAULT N'Missing',
        [OutboundCredentialImportedAtUtc] datetime2 NULL,
        [OutboundLastTokenReceivedAtUtc] datetime2 NULL,
        [Description] nvarchar(2000) NULL,
        [Status] nvarchar(32) NOT NULL DEFAULT N'Pending',
        [IsEnabled] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_RuntimeDesignNodes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191225_InitialRuntimeStorage'
)
BEGIN
    CREATE INDEX [IX_RuntimeContractArtifacts_ArtifactType_Topic_DeployedAtUtc] ON [RuntimeContractArtifacts] ([ArtifactType], [Topic], [DeployedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191225_InitialRuntimeStorage'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuntimeContractArtifacts_ArtifactType_Topic_VersionNumber] ON [RuntimeContractArtifacts] ([ArtifactType], [Topic], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191225_InitialRuntimeStorage'
)
BEGIN
    CREATE INDEX [IX_RuntimeContractArtifacts_SourceReleaseId] ON [RuntimeContractArtifacts] ([SourceReleaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191225_InitialRuntimeStorage'
)
BEGIN
    CREATE INDEX [IX_RuntimeDesignNodes_InboundClientId] ON [RuntimeDesignNodes] ([InboundClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191225_InitialRuntimeStorage'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuntimeDesignNodes_Key] ON [RuntimeDesignNodes] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191225_InitialRuntimeStorage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260905191225_InitialRuntimeStorage', N'10.0.3');
END;

COMMIT;
GO

