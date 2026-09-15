# KnOwl Distribution And Runtime Implementation Roadmap

## Goal

Finish KnOwl distribution and runtime using the Krackend orchestration control-plane/runtime pattern, adapted to KnOwl contracts metadata.

KnOwl runtime does not execute live instances. It only stores deployed contract metadata artifacts and exposes them for backend services.

## Decisions

- Control Plane and Runtime will run as separate hosts.
- Only Event and Command versions generate deployable artifacts.
- Data Types and Custom Fields are not deployed as standalone artifacts; they remain embedded as snapshots in Event and Command artifact payloads.
- Deploying an approved Event or Command version must create the artifact automatically.
- Releases are delivery bundles only. Releases do not have review or approval lifecycle.
- Distribution security will follow the Krackend client-credentials pattern: credential exchange, token issuance, bearer validation, scopes and short-lived cached tokens.
- Runtime will expose REST endpoints immediately and leave gRPC contracts ready for exact-version and latest artifact lookup.

## Implementation Steps

1. [x] Document the mapping from Krackend to KnOwl.
2. [x] Create/complete shared distribution security primitives.
3. [x] Extend Runtime Node storage with outbound/inbound credential state required by the Krackend security flow.
4. [x] Update Control Plane distribution services to authenticate push and pull requests.
5. [x] Create a separate KnOwl Runtime host.
6. [x] Implement Runtime token issuance/validation and secured artifact deploy endpoint.
7. [x] Implement Runtime artifact query endpoints for exact version and latest deployed.
8. [x] Prepare gRPC contracts and service registrations for artifact lookup.
9. [x] Update migrations and SQL scripts for the new distribution/runtime fields.
10. [x] Add unit tests for security, artifact delivery, deployment idempotency and latest lookup.
11. [x] Add integration tests for secured Control Plane to Runtime delivery.
12. [x] Add local Docker infrastructure for e2e validation.
13. [x] Run build, unit tests, integration tests and e2e smoke tests.

## Krackend Mapping

| Krackend Concept | KnOwl Equivalent |
| --- | --- |
| Orchestration version lifecycle | Event/Command version lifecycle |
| Orchestration artifact | Contract artifact |
| Design/Control Plane host | KnOwl web host |
| Runtime host | KnOwl Runtime host |
| Runtime artifact deploy endpoint | Runtime contract artifact deploy endpoint |
| Artifact payload | Contract metadata payload with schema snapshots |
| Runtime projection/activation | Runtime metadata storage only |
| Distribution assignment/release target | Contract release target |
| Client credential connection security | KnOwl runtime node credential security |

## Validation

- `dotnet build KnOwl.slnx --no-restore`
- `dotnet test KnOwl.slnx --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-knowl-distribution-e2e.ps1`

## Delivered State

- Control Plane remains the KnOwl web host and owns design-time contracts, artifacts, release bundles, runtime environments and runtime nodes.
- Runtime Host is a separate ASP.NET host that stores deployed artifact metadata and exposes REST/gRPC read paths.
- Event/Command version deployment creates immutable artifacts; releases only bundle and deliver those artifacts.
- Runtime Node credentials are exchanged through generated JSON/Base64 packages.
- Push delivery is secured with client credentials and bearer scopes.
- Pull delivery endpoints are secured and ready for runtime-initiated sync flows.
- Runtime storage has its own DbContext and SQL migration history.
- Data Types and Custom Fields are not standalone deployment artifacts; they remain embedded in Event/Command payload snapshots.
