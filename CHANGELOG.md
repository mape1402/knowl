# Changelog

## [v1.0.4]

- Adds packable `KnOwl.ControlPlane.Api` and `KnOwl.Runtime.Api` libraries for host-agnostic Minimal API exposure.
- Exposes Control Plane REST endpoints for schema types, metadata fields, events, commands, artifacts, runtime environments, runtime nodes, credentials, and releases.
- Exposes Runtime REST endpoints for status, deployed artifacts, command request/reply artifacts, Control Plane connections, pending pull artifacts, and artifact application.
- Adds optional `ApiAuthorizationPolicy` bootstrap configuration so hosts can secure REST endpoints without KnOwl choosing an authentication provider.
- Updates README documentation with the REST API package map, route catalog, and command request/reply creation example.
- Adds endpoint tests for Control Plane command request/reply creation and Runtime command artifact responses.

## [v1.0.3]

- Adds Date, DateTime, Time, and TimeSpan schema types to the Control Plane designer flow.
- Adds Runtime-to-Control Plane connection setup in the sample Runtime host.
- Splits command versions into required request schemas and optional reply schemas while preserving existing request definitions.
- Separates deployed schema catalog responses for events and commands, including command request and reply artifacts.
- Removes Helm, Kubernetes, and generated SQL deployment artifacts from the NuGet library repository.
- Expands the README with badges, Getting Started guidance, package map, and catalog usage.
- Keeps KnOwl bootstrap connection configuration host-agnostic by removing Managed Identity-specific connection-string rewriting.

## [v1.0.2]

- Exposes deployed contract schemas from Control Plane hosts through contract catalog endpoints.
- Adds lookup endpoints for all deployed artifacts, exact contract versions, and latest contract versions.
- Limits Control Plane catalog responses to generated artifacts whose source status is `Deployed`.
- Keeps Runtime contract catalog endpoints unchanged.
- Adds service, endpoint, integration, and e2e coverage for Control Plane contract consumption.

## [v1.0.1]

- First production-ready KnOwl release with reusable `KnOwl.*` libraries for contract design, promotion, distribution, runtime catalog, storage, security, Web UI, bootstrap, and worker behavior.
- Thin Control Plane and Runtime host samples with EF Core migrations kept in the host projects.
- Multi-target support for `net9.0` and `net10.0`.
- SQL Server integration and end-to-end distribution validation for push and pull runtime flows.
- NuGet package metadata for all packable KnOwl libraries.

## [v1.0.0]

- First production-ready KnOwl release with reusable `KnOwl.*` libraries for contract design, promotion, distribution, runtime catalog, storage, security, Web UI, bootstrap, and worker behavior.
- Thin Control Plane and Runtime host samples with EF Core migrations kept in the host projects.
- Multi-target support for `net9.0` and `net10.0`.
- SQL Server integration and end-to-end distribution validation for push and pull runtime flows.
- NuGet package metadata for all packable KnOwl libraries.

## [0.0.0]

- Initial KnOwl extraction baseline.
