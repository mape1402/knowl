BEGIN TRANSACTION;
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

CREATE UNIQUE INDEX [IX_RuntimeContractArtifacts_ArtifactType_Topic_VersionNumber] ON [RuntimeContractArtifacts] ([ArtifactType], [Topic], [VersionNumber]);

CREATE INDEX [IX_RuntimeContractArtifacts_SourceReleaseId] ON [RuntimeContractArtifacts] ([SourceReleaseId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725201635_AddRuntimeContractArtifacts', N'10.0.3');

COMMIT;
GO

