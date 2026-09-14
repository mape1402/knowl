# KnOwl Restructure Like Krackend

## Objective

Restructure KnOwl to follow the same separation used by Krackend orchestration modules: shared contracts, control-plane domain, application services, Entity Framework storage, WebUI modules, and executable hosts as composition roots.

ButterMorph stays in place as the designer surface. The restructuring must not replace the existing functionality or change the database schema.

## Constraints

- Do not replace ButterMorph.
- Do not change Managed Identity behavior.
- Do not change the database schema during this restructuring.
- Keep the existing EF Core migration history valid.
- Keep `KnOwl` as the control-plane executable host.
- Keep `KnOwl.RuntimeHost` as the runtime executable host.
- Verify build and tests after each structural phase.

## Target Structure

- `KnOwl.Contracts`: shared artifact delivery DTOs, security contracts, token helpers, `ContractArtifact`, `ContractArtifactType`, and shared distribution enums.
- `KnOwl.ControlPlane`: control-plane domain for design and distribution, including events, commands, data types, custom fields, releases, runtime environments, runtime nodes, and repository contracts.
- `KnOwl.ControlPlane.Application`: control-plane application services for design and distribution workflows.
- `KnOwl.ControlPlane.Storage.EntityFramework`: SQL Server persistence for control-plane design and distribution workflows.
- `KnOwl.ControlPlane.WebUI`: Razor Pages and ButterMorph integration for the KnOwl designer/control plane.
- `KnOwl.Runtime`: runtime metadata domain and repository contracts.
- `KnOwl.Runtime.Application`: runtime artifact delivery, catalog, deployment, and security services.
- `KnOwl.Runtime.Storage.EntityFramework`: SQL Server persistence for runtime metadata.
- `KnOwl.Runtime.WebUI`: runtime Razor Pages.
- `KnOwl`: control-plane host and composition root.
- `KnOwl.RuntimeHost`: runtime host and composition root.

## Validation Plan

- Run `dotnet restore KnOwl.slnx`.
- Run `dotnet build KnOwl.slnx --no-restore`.
- Run `dotnet test KnOwl.slnx --no-build --verbosity minimal`.
- Start the control-plane host locally and validate `/health`, Events, Commands, Data Types, Custom Fields, Runtime Nodes, Runtime Environments, Artifacts, and Releases pages.
- Start the runtime host locally and validate `/health`, runtime dashboard, deployed artifacts catalog, REST catalog endpoints, and gRPC host startup.
- Execute an end-to-end distribution flow from control plane to runtime using local SQL Server infrastructure.

## Migration Notes

- EF migrations remain in the host projects and continue using the existing migrations assemblies.
- The model namespaces in migration snapshots were updated to the new CLR namespaces, but table names and columns were not changed.
- Runtime does not depend on control-plane application or storage. It only consumes shared contracts and runtime libraries.