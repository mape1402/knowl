BEGIN TRANSACTION;
CREATE TABLE [ContractReleases] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(32) NOT NULL DEFAULT N'Draft',
    [CreatedAtUtc] datetime2 NOT NULL,
    [InReviewAtUtc] datetime2 NULL,
    [ApprovedAtUtc] datetime2 NULL,
    [DeployedAtUtc] datetime2 NULL,
    [CanceledAtUtc] datetime2 NULL,
    CONSTRAINT [PK_ContractReleases] PRIMARY KEY ([Id])
);

CREATE TABLE [ContractReleaseItems] (
    [Id] uniqueidentifier NOT NULL,
    [ReleaseId] uniqueidentifier NOT NULL,
    [ArtifactId] uniqueidentifier NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ContractReleaseItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContractReleaseItems_ContractArtifacts_ArtifactId] FOREIGN KEY ([ArtifactId]) REFERENCES [ContractArtifacts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContractReleaseItems_ContractReleases_ReleaseId] FOREIGN KEY ([ReleaseId]) REFERENCES [ContractReleases] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ContractReleaseItems_ArtifactId] ON [ContractReleaseItems] ([ArtifactId]);

CREATE UNIQUE INDEX [IX_ContractReleaseItems_ReleaseId_ArtifactId] ON [ContractReleaseItems] ([ReleaseId], [ArtifactId]);

CREATE INDEX [IX_ContractReleases_Status] ON [ContractReleases] ([Status]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725201049_AddContractReleases', N'10.0.3');

COMMIT;
GO

