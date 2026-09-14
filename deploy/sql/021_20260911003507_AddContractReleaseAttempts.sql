BEGIN TRANSACTION;
CREATE TABLE [ContractReleaseAttempts] (
    [Id] uniqueidentifier NOT NULL,
    [ReleaseTargetId] uniqueidentifier NOT NULL,
    [Action] nvarchar(64) NOT NULL,
    [InitiatedBy] nvarchar(128) NOT NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [FinishedAtUtc] datetime2 NULL,
    [Succeeded] bit NOT NULL,
    [ErrorCode] nvarchar(128) NOT NULL,
    [ErrorMessage] nvarchar(2048) NOT NULL,
    [ExternalReference] nvarchar(256) NOT NULL,
    CONSTRAINT [PK_ContractReleaseAttempts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContractReleaseAttempts_ContractReleaseTargets_ReleaseTargetId] FOREIGN KEY ([ReleaseTargetId]) REFERENCES [ContractReleaseTargets] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ContractReleaseAttempts_ReleaseTargetId_StartedAtUtc] ON [ContractReleaseAttempts] ([ReleaseTargetId], [StartedAtUtc]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260911003507_AddContractReleaseAttempts', N'10.0.3');

COMMIT;
GO

