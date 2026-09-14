# KnOwl Contract Promotion Roadmap

## Objective

Implement an Orchestrator-like promotion, distribution, release, and runtime workflow for KnOwl async contracts.

KnOwl design catalogs (`Custom Types` and `Custom Fields`) remain design-time tools. Runtime artifacts are generated only from versioned `Events` and `Commands` because their payload schemas already include embedded snapshots of referenced custom types and field metadata.

## Decisions

- Runtime only stores deployed `EventVersion` and `CommandVersion` artifacts.
- `Custom Types` and `Custom Fields` are not deployed as runtime artifacts.
- `Custom Types` and `Custom Fields` can be referenced while designing Events and Commands before anything is deployed.
- Event and Command versions must persist complete payload snapshots, including `$defs` and metadata values.
- References such as `typeId` and `typeVersionId` are traceability metadata, not runtime lookup dependencies.
- Nothing is physically deleted.
- Versions are immutable once promoted beyond draft.
- Promotion is blocked if a referenced snapshot/dependency is missing or inconsistent.
- Runtime storage does not execute anything; it only stores and exposes deployed contract artifacts.

## Lifecycle Model

### Definition Status

Definitions are global records such as `Event`, `Command`, `Custom Type`, and `Custom Field`.

Allowed definition states:

- `Active`
- `Inactive`

No definition can be physically deleted.

### Version Status

Applies to `EventVersion` and `CommandVersion` for promotion/deployment.

Allowed version states:

- `Draft`
- `InReview`
- `Approved`
- `Deployed`
- `Deprecated`
- `Archived`

`Custom Type` and `Custom Field` versions remain versioned design catalog records. They can be active/inactive but are not promoted to runtime artifacts.

### Release Status

Allowed release states:

- `Draft`
- `InReview`
- `Approved`
- `Deployed`
- `Canceled`

## Runtime Artifact Scope

Runtime artifacts are generated from:

- `EventVersion`
- `CommandVersion`

Runtime artifacts are not generated from:

- `SchemaTypeVersion`
- `ContractFieldMetadataVersion`

## Artifact Shape

A runtime artifact should include enough data for backend services to consume it without querying KnOwl design catalogs.

Required artifact metadata:

- Artifact id
- Artifact type: `event` or `command`
- Definition id
- Version id
- Key/name/topic
- Version number
- Description
- Payload schema snapshot JSON
- Content hash
- Source status at artifact creation
- Created timestamp
- Release/deployment metadata when applicable

## Promotion Validation Rules

Promotion must be blocked when:

- A schema contains `$ref` to `#/$defs/{name}@{version}` but that key is missing from `$defs`.
- A schema contains `typeVersionId` but the referenced design version does not exist.
- A referenced design version exists but its embedded snapshot does not match the stored referenced schema, if hash validation is enabled.
- The same release includes duplicate artifacts for the same `artifactType + key + version`.
- Runtime already contains the same `artifactType + key + version` with a different hash.
- A version is not in a promotable state.

Promotion should not be blocked when:

- The custom type or custom field definition is inactive, as long as the referenced version and snapshot exist.
- A newer custom type or custom field version exists.
- The referenced custom type or custom field has not been deployed, because those records are design-time only.

## Roadmap

### Phase 1 - Analyze Orchestrator Workflow

Tasks:

- Review `C:\dmx\Dmx.Orchestrator` design/distribution/runtime projects.
- Identify Orchestrator promotion entities, release entities, runtime storage entities, and status transitions.
- Map equivalent KnOwl concepts without copying execution-specific runtime behavior.

Validation:

- Document findings in this roadmap or a companion note.
- Confirm KnOwl-specific deviations are intentional.

Commit:

- `Document KnOwl promotion workflow findings`

### Phase 2 - Add Lifecycle State To Design Versions

Tasks:

- Add lifecycle status to `EventVersion` and `CommandVersion`.
- Add status timestamps where useful, for example submitted/approved/deployed timestamps.
- Keep existing records backward-compatible by defaulting current versions to `Draft` or an agreed initial state.
- Disable physical delete behavior in UI/actions for Events and Commands, replacing it with inactive/deprecated behavior.

Validation:

- `dotnet build KnOwl.slnx`
- EF migration generated and reviewed.
- SQL script generated with versioned order.

Commit:

- `Add lifecycle state to contract versions`

### Phase 3 - Add Promotion Interaction

Tasks:

- Add services for submitting, approving, rejecting, deploying, and deprecating Event/Command versions.
- Enforce allowed state transitions.
- Keep mutation logic out of Razor Pages.
- Add unit tests for state transitions.

Validation:

- `dotnet test KnOwl.slnx`
- Tests cover valid and invalid transitions.

Commit:

- `Add contract promotion interaction services`

### Phase 4 - Add Snapshot Integrity Validator

Tasks:

- Implement validator for Event/Command payload schema snapshots.
- Validate `$ref` to `$defs` consistency.
- Validate `typeVersionId` references exist in design storage.
- Optionally compare embedded snapshot hash against design type version schema.
- Return actionable validation errors for UI display.

Validation:

- Unit tests for missing `$defs`, missing type versions, valid embedded snapshots, and duplicate references.
- `dotnet test KnOwl.slnx`

Commit:

- `Add contract snapshot promotion validation`

### Phase 5 - Add Distribution Artifacts

Tasks:

- Create KnOwl distribution project following Orchestrator separation.
- Add artifact model and storage interfaces.
- Add SQL Server distribution storage implementation.
- Generate immutable artifacts from approved Event/Command versions.
- Compute deterministic content hash from artifact payload.

Validation:

- EF migration generated and reviewed.
- Unit tests for artifact creation and hash stability.
- `dotnet test KnOwl.slnx`

Commit:

- `Add KnOwl contract distribution artifacts`

### Phase 6 - Add Releases And Bundles

Tasks:

- Add release model.
- Add release item model for selected artifacts.
- Allow releases to bundle multiple Event/Command artifacts.
- Validate duplicate/conflicting artifact selections.
- Add release state transitions.

Validation:

- EF migration generated and reviewed.
- Unit tests for release validation and state transitions.
- `dotnet test KnOwl.slnx`

Commit:

- `Add KnOwl contract releases`

### Phase 7 - Add Runtime Storage

Tasks:

- Create KnOwl runtime storage project following Orchestrator separation.
- Add runtime deployed artifact model.
- Add SQL Server runtime storage implementation.
- Deploy approved release artifacts into runtime storage.
- Prevent overwrite when same artifact key/version exists with different hash.

Validation:

- EF migration generated and reviewed.
- Unit tests for idempotent deploy and conflict blocking.
- `dotnet test KnOwl.slnx`

Commit:

- `Add KnOwl runtime contract storage`

### Phase 8 - Add UI Flows

Tasks:

- Add views/actions to submit Event/Command versions for approval.
- Add approval/rejection screens.
- Add artifact list screen.
- Add release creation screen with artifact selection.
- Add release deploy screen and runtime artifact list.
- Remove or replace delete actions with deactivate/deprecate flows.

Validation:

- `dotnet build KnOwl.slnx`
- Manual local run to verify UI flow.
- Browser smoke test for Events, Commands, Artifacts, Releases, and Runtime.

Commit:

- `Add KnOwl promotion and release UI`

### Phase 9 - SQL Scripts And Deployment Readiness

Tasks:

- Generate ordered SQL scripts for all new migrations.
- Update deployment notes for applying schema changes before application rollout.
- Ensure health checks remain valid.
- Ensure no startup migration execution is introduced.

Validation:

- SQL scripts are ordered and named consistently.
- `dotnet build KnOwl.slnx`
- `dotnet test KnOwl.slnx`

Commit:

- `Add promotion rollout SQL scripts`

## Open Questions

- Should existing Event/Command versions start as `Draft`, `Approved`, or `Deployed` during migration?
- Should hash validation against design custom type versions be strict blocking in phase one, or only validate `$defs` presence first?
- Should runtime expose read APIs now, or only storage and UI list in the first implementation?
- Should release approval be required before deploy, or can approved artifacts be deployed directly in development environments?

