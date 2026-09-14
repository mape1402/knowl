# KnOwl Promotion Workflow Findings From Orchestrator

## Orchestrator Pattern Reviewed

Source reviewed: `C:\dmx\Dmx.Orchestrator`.

Relevant projects:

- `Orchestrator.Design`
- `Orchestrator.Design.Interaction`
- `Orchestrator.Design.Storage.SqlServer`
- `Orchestrator.Distribution`
- `Orchestrator.Distribution.Interaction`
- `Orchestrator.Distribution.Storage.SqlServer`
- `Orchestrator.Runtime`
- `Orchestrator.Runtime.Interaction`
- `Orchestrator.Runtime.Storage.SqlServer`

## Design Lifecycle

Orchestrator models deployable units as versions. The version owns lifecycle status and approval/deployment timestamps.

Observed version statuses:

- `Draft`
- `InReview`
- `Approved`
- `Deployed`
- `Deprecated`
- `Archived`

Observed transitions:

- `Draft` -> `InReview`
- `InReview` -> `Draft`
- `InReview` -> `Approved`
- `Approved` -> `InReview`
- `Approved` -> `Deployed`
- `Deployed` -> `Deprecated`
- `Deprecated` -> `Archived`

KnOwl decision:

- Use the same status names for `EventVersion` and `CommandVersion`.
- Do not use `Rejected`; returning to draft covers review rejection/rework.
- Use `Archived` as a terminal non-deleted state.

## Artifact Generation

Orchestrator generates artifacts from deployed orchestration versions by building a full snapshot payload and publishing a lifecycle event. Distribution receives that event and stores an immutable artifact.

KnOwl decision:

- Generate artifacts from `EventVersion` and `CommandVersion` only.
- The artifact payload is the full payload schema snapshot already stored on the version.
- Custom types and custom fields stay design-time only.
- Runtime never resolves live design catalog references.

## Distribution

Orchestrator distribution stores artifacts and releases. Its current release model is single-artifact oriented in the observed files, but KnOwl needs multi-artifact bundles.

KnOwl decision:

- Implement KnOwl releases as bundle-first.
- A release can contain multiple event/command artifacts.
- A release item references one immutable artifact.
- Duplicate `artifactType + key + version` selections are blocked.

## Runtime

Orchestrator runtime materializes accepted artifacts into runtime storage and activates deploy artifacts. KnOwl runtime is simpler.

KnOwl decision:

- Runtime stores deployed contract artifacts only.
- Runtime does not execute workflows, dispatch tasks, or process triggers.
- Runtime deployment is idempotent when the same artifact key/version/hash already exists.
- Runtime deployment is blocked when the same artifact key/version exists with a different hash.

## KnOwl Implementation Deviations

KnOwl intentionally differs from Orchestrator in these areas:

- No runtime execution engine.
- No runtime nodes/environments in the first implementation unless deployment targeting becomes necessary.
- No artifact generation for design catalog records (`SchemaTypeVersion`, `ContractFieldMetadataVersion`).
- Releases are multi-artifact bundles from the start.
- Snapshot integrity validation is required before promotion/deployment because schemas can reference design catalog versions while embedding their snapshots.

## Next Implementation Step

Add lifecycle fields to `EventVersion` and `CommandVersion`, then generate the EF migration. Existing rows should default to `Draft` unless a production migration strategy says otherwise.
