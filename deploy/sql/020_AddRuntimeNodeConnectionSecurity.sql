BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [AccessTokenTtlSeconds] int NOT NULL DEFAULT 86400;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [DeletedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundAllowedScopes] nvarchar(500) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundClientId] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundCredentialCreatedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundCredentialRevokedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundCredentialRotatedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundCredentialStatus] nvarchar(32) NOT NULL DEFAULT N'Missing';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundKeyId] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundLastFailureReason] nvarchar(2000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundLastTokenFailedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundLastTokenIssuedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [InboundSecretHash] nvarchar(500) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundClientId] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundCredentialImportedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundCredentialStatus] nvarchar(32) NOT NULL DEFAULT N'Missing';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundKeyId] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundLastTokenReceivedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [OutboundRequestedScopes] nvarchar(500) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [ProtectedOutboundSecret] nvarchar(2000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [TokenRefreshSkewSeconds] int NOT NULL DEFAULT 300;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    ALTER TABLE [RuntimeNodes] ADD [TokenValidationCacheTtlSeconds] int NOT NULL DEFAULT 300;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    CREATE INDEX [IX_RuntimeNodes_InboundClientId] ON [RuntimeNodes] ([InboundClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905191117_AddRuntimeNodeConnectionSecurity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260905191117_AddRuntimeNodeConnectionSecurity', N'10.0.3');
END;

COMMIT;
GO

