BEGIN TRANSACTION;

INSERT INTO [dbo].[Events] (
    [Id],
    [Name],
    [Topic],
    [Description],
    [CreatedAtUtc],
    [UpdatedAtUtc]
)
SELECT
    [Id],
    [Name],
    [Topic],
    [Description],
    [CreatedAtUtc],
    [UpdatedAtUtc]
FROM [dbo].[EventsBackup]
WHERE [Id] NOT IN (
    SELECT [Id]
    FROM [dbo].[Events]
);

INSERT INTO [dbo].[EventVersions] (
    [Id],
    [EventDefinitionId],
    [VersionNumber],
    [PayloadSchemaJson],
    [CreatedAtUtc],
    [UpdatedAtUtc],
    [Comment]
)
SELECT
    [Id],
    [EventDefinitionId],
    [VersionNumber],
    [PayloadSchemaJson],
    [CreatedAtUtc],
    [UpdatedAtUtc],
    [Comment]
FROM [dbo].[EventVersionsBackup]
WHERE [Id] NOT IN (
    SELECT [Id]
    FROM [dbo].[EventVersions]
);

COMMIT;
