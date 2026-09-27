# Changelog

## [v2.2.1]

- Updates ButterMorph dependencies to `2.0.0` for the Control Plane Web UI and bootstrap packages.
- Validates the existing KnOwl ButterMorph adapter behavior against the new ButterMorph release.

## [v2.2.0]

- Aligns the Runtime Web UI with the Control Plane shell, including collapsible sidebar navigation, card-based lists, overview content, search, and consistent English text.
- Updates Documentation spaces, topics, pages, and page versions to use the same card-driven browsing experience as the rest of KnOwl.
- Refines Distribution artifact browsing so released events and commands are grouped first, with artifact details opened from focused cards.
- Improves card sizing, contextual menus, message boxes, toasts, and dark-mode contrast across Control Plane and Runtime screens.
- Adds Runtime sidebar brand color support and keeps the brand header aligned with the injected host theme when no separate brand color is configured.
- Adds tests for Runtime Web UI markup, artifact grouping, and theme variable behavior.

## [v2.1.0]

- Adds consistent search bars across Control Plane operational views for events, commands, data types, custom fields, environments, runtime nodes, artifacts, releases, and version details.
- Adds search support to Runtime Control Plane connections while preserving the existing Runtime artifact filters.
- Adds page search to Documentation topics so spaces, topics, pages, and page versions can all be filtered from the reusable UI.
- Keeps search state in query strings so filtered views can be refreshed, linked, and cleared without losing the current context.
- Adds tests for artifact grouping search and runtime connection/runtime node filtering.

## [v2.0.0]

- Adds host-configurable UI branding for Control Plane and Runtime bootstrap options, including title, sidebar icon, and theme colors.
- Keeps the default KnOwl Web UI purple and white while allowing hosts to override the theme without modifying library assets.
- Updates the sample Control Plane and Runtime hosts with blue sample branding and the KnOwl icon.
- Removes visible environment labels from the reusable Web UI layouts.
- Aligns sample design-time DbContext factories with the sample databases so EF migrations update the same databases used by the running hosts.

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
