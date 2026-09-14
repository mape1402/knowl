BEGIN TRANSACTION;
ALTER TABLE [EventVersions] ADD [ApprovedAtUtc] datetime2 NULL;

ALTER TABLE [EventVersions] ADD [ArchivedAtUtc] datetime2 NULL;

ALTER TABLE [EventVersions] ADD [DeployedAtUtc] datetime2 NULL;

ALTER TABLE [EventVersions] ADD [DeprecatedAtUtc] datetime2 NULL;

ALTER TABLE [EventVersions] ADD [InReviewAtUtc] datetime2 NULL;

ALTER TABLE [EventVersions] ADD [Status] nvarchar(32) NOT NULL DEFAULT N'Draft';

ALTER TABLE [Events] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);

ALTER TABLE [CommandVersions] ADD [ApprovedAtUtc] datetime2 NULL;

ALTER TABLE [CommandVersions] ADD [ArchivedAtUtc] datetime2 NULL;

ALTER TABLE [CommandVersions] ADD [DeployedAtUtc] datetime2 NULL;

ALTER TABLE [CommandVersions] ADD [DeprecatedAtUtc] datetime2 NULL;

ALTER TABLE [CommandVersions] ADD [InReviewAtUtc] datetime2 NULL;

ALTER TABLE [CommandVersions] ADD [Status] nvarchar(32) NOT NULL DEFAULT N'Draft';

ALTER TABLE [Commands] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);

CREATE INDEX [IX_EventVersions_Status] ON [EventVersions] ([Status]);

CREATE INDEX [IX_Events_IsActive] ON [Events] ([IsActive]);

CREATE INDEX [IX_CommandVersions_Status] ON [CommandVersions] ([Status]);

CREATE INDEX [IX_Commands_IsActive] ON [Commands] ([IsActive]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725195512_AddContractLifecycleState', N'10.0.3');

COMMIT;
GO

