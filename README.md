# KnOwl

KnOwl provides reusable .NET libraries for contract design, promotion, distribution, and runtime contract metadata hosting.

The repository is organized like the Krackend modular packages: all behavior lives in libraries, while executable projects are samples or deployment hosts.

## Release

Current release: `1.0.0`

KnOwl packages target both `net9.0` and `net10.0`. The shared package version is defined in `Directory.Build.props`, while `.release` is the release marker used by the deployment workflow.

## Packages

- `KnOwl.Contracts`
- `KnOwl.ControlPlane`
- `KnOwl.ControlPlane.Application`
- `KnOwl.ControlPlane.Storage.EntityFramework`
- `KnOwl.ControlPlane.WebUI`
- `KnOwl.ControlPlane.Bootstrap`
- `KnOwl.Runtime`
- `KnOwl.Runtime.Application`
- `KnOwl.Runtime.Storage.EntityFramework`
- `KnOwl.Runtime.WebUI`
- `KnOwl.Runtime.Bootstrap`

## Samples

- `samples/KnOwl.ControlPlaneHost.Sample`
- `samples/KnOwl.RuntimeHost.Sample`

The samples are intentionally thin hosts. Database migrations live in the hosts, while reusable domain, application, storage, UI, endpoint, worker, and bootstrap behavior lives in the `KnOwl.*` libraries.

## Contract Catalog

Runtime hosts expose deployed local artifacts through `/runtime/contracts/*`.

Control Plane hosts also expose deployed source artifacts, limited to contracts whose generated artifact has `SourceStatus` equal to `Deployed`:

- `GET /contracts/artifacts`
- `GET /contracts/{artifactType}/{topic}/versions/{versionNumber}`
- `GET /contracts/{artifactType}/{topic}/latest`

## Build

```powershell
dotnet restore KnOwl.slnx
dotnet build KnOwl.slnx --no-restore
dotnet test KnOwl.slnx --no-build
```

## Package

```powershell
Get-ChildItem src -Recurse -Filter *.csproj | ForEach-Object {
  dotnet pack $_.FullName --configuration Release -o artifacts/packages
}
```

## End-to-end

The distribution e2e runner starts SQL Server in Docker, runs the Control Plane and Runtime samples, and validates SQL Server storage plus push/pull artifact distribution flows for both target frameworks.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-knowl-distribution-e2e.ps1
```
