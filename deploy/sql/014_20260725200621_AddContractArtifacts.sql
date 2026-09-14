BEGIN TRANSACTION;
CREATE TABLE [ContractArtifacts] (
    [Id] uniqueidentifier NOT NULL,
    [ArtifactType] nvarchar(32) NOT NULL,
    [DefinitionId] uniqueidentifier NOT NULL,
    [VersionId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Topic] nvarchar(70) NOT NULL,
    [VersionNumber] nvarchar(13) NOT NULL,
    [Description] nvarchar(max) NULL,
    [PayloadSchemaJson] nvarchar(max) NOT NULL,
    [ContentHash] nvarchar(128) NOT NULL,
    [SourceStatus] nvarchar(32) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ContractArtifacts] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_ContractArtifacts_ArtifactType_Topic_VersionNumber] ON [ContractArtifacts] ([ArtifactType], [Topic], [VersionNumber]);

CREATE UNIQUE INDEX [IX_ContractArtifacts_ArtifactType_VersionId] ON [ContractArtifacts] ([ArtifactType], [VersionId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725200621_AddContractArtifacts', N'10.0.3');

COMMIT;
GO

