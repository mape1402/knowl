# KnOwl

[![Build and Release](https://github.com/mape1402/knowl/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/mape1402/knowl/actions/workflows/build-and-release.yml)
[![NuGet](https://img.shields.io/nuget/v/KnOwl.Contracts.svg)](https://www.nuget.org/packages/KnOwl.Contracts)
[![NuGet Downloads](https://img.shields.io/nuget/dt/KnOwl.Contracts.svg)](https://www.nuget.org/packages/KnOwl.Contracts)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/mape1402/knowl)

KnOwl is a set of reusable .NET libraries for designing, versioning, promoting, distributing, and consuming async contract metadata. It gives teams a Control Plane where contracts are authored and released, plus a Runtime surface where deployed contracts can be consumed by running services.

The goal is to keep product hosts thin. Your app owns the executable project, configuration, and EF migrations; KnOwl packages provide the domain model, application services, storage adapters, Razor UI, catalog endpoints, distribution endpoints, and runtime synchronization behavior.

## What KnOwl Solves

- Design event and command contracts with versioned payload schemas.
- Model commands with a required request schema and optional reply schema.
- Promote versions through lifecycle states and generate immutable artifacts.
- Distribute deployed artifacts from a Control Plane to one or more Runtime hosts.
- Expose deployed schemas through split event and command catalog endpoints.
- Keep host-specific database migrations outside the reusable NuGet libraries.

## Packages

Install only the layer your host needs:

| Package | Purpose |
| --- | --- |
| `KnOwl.Contracts` | Shared DTOs for artifacts, delivery, catalog responses, and security. |
| `KnOwl.ControlPlane` | Control Plane domain model and repository contracts. |
| `KnOwl.ControlPlane.Application` | Control Plane services for design, lifecycle, artifacts, releases, and delivery. |
| `KnOwl.ControlPlane.Storage.EntityFramework` | EF Core storage for Control Plane state. |
| `KnOwl.ControlPlane.WebUI` | Reusable Razor UI for Control Plane hosts. |
| `KnOwl.ControlPlane.Bootstrap` | ASP.NET Core composition for Control Plane hosts. |
| `KnOwl.Runtime` | Runtime domain model and repository contracts. |
| `KnOwl.Runtime.Application` | Runtime catalog, deployment, pull, and security services. |
| `KnOwl.Runtime.Storage.EntityFramework` | EF Core storage for Runtime state. |
| `KnOwl.Runtime.WebUI` | Reusable Razor UI for Runtime hosts. |
| `KnOwl.Runtime.Bootstrap` | ASP.NET Core composition for Runtime hosts. |

All packages target `net9.0` and `net10.0`.

## Getting Started

### 1. Create a Control Plane host

```powershell
dotnet new web -n MyCompany.Contracts.ControlPlane
cd MyCompany.Contracts.ControlPlane

dotnet add package KnOwl.ControlPlane.Bootstrap
dotnet add package KnOwl.ControlPlane.Storage.EntityFramework
```

Use the bootstrap package in `Program.cs`:

```csharp
using KnOwl.ControlPlane.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKnOwlControlPlane(
    builder.Configuration,
    options => options.MigrationsAssembly = typeof(Program).Assembly.GetName().Name);

var app = builder.Build();

app.MapKnOwlControlPlane();

app.Run();
```

Add configuration:

```json
{
  "ConnectionStrings": {
    "KnOwlDb": "Server=localhost;Database=KnOwlControlPlane;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Create migrations in the host project:

```powershell
dotnet ef migrations add InitialKnOwlControlPlane `
  --context KnOwlDbContext `
  --output-dir Migrations

dotnet ef database update --context KnOwlDbContext
```

Run the host and open the Control Plane UI. From there you can create data types, custom metadata fields, events, commands, versions, artifacts, runtime environments, runtime nodes, and releases.

### 2. Create a Runtime host

```powershell
dotnet new web -n MyCompany.Contracts.Runtime
cd MyCompany.Contracts.Runtime

dotnet add package KnOwl.Runtime.Bootstrap
dotnet add package KnOwl.Runtime.Storage.EntityFramework
```

Use the runtime bootstrap in `Program.cs`:

```csharp
using KnOwl.Runtime.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKnOwlRuntime(
    builder.Configuration,
    options => options.MigrationsAssembly = typeof(Program).Assembly.GetName().Name);

var app = builder.Build();

app.MapKnOwlRuntime();

app.Run();
```

Add configuration:

```json
{
  "ConnectionStrings": {
    "KnOwlRuntimeDb": "Server=localhost;Database=KnOwlRuntime;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Runtime": {
    "ArtifactPull": {
      "Enabled": true,
      "InitialDelaySeconds": 5,
      "IntervalSeconds": 30
    }
  }
}
```

Create runtime migrations in the host project:

```powershell
dotnet ef migrations add InitialKnOwlRuntime `
  --context KnOwlRuntimeDbContext `
  --output-dir Migrations/RuntimeStorage

dotnet ef database update --context KnOwlRuntimeDbContext
```

### 3. Connect Runtime to Control Plane

1. In the Runtime UI, create a Control Plane connection.
2. Generate or import the connection credentials.
3. In the Control Plane UI, register the Runtime node and credentials.
4. Release deployed artifacts from the Control Plane.
5. Let the Runtime pull pending artifacts, or push artifacts to the Runtime delivery endpoint.

The sample hosts show the intended shape:

- `samples/KnOwl.ControlPlaneHost.Sample`
- `samples/KnOwl.RuntimeHost.Sample`

## Contract Catalog

Control Plane hosts expose deployed source artifacts only when their artifact status is deployed:

- `GET /contracts/artifacts`
- `GET /contracts/events/{eventKey}/versions/{versionNumber}`
- `GET /contracts/commands/{commandKey}/versions/{versionNumber}`

Runtime hosts expose deployed local artifacts:

- `GET /runtime/contracts/artifacts`
- `GET /runtime/contracts/events/{eventKey}/versions/{versionNumber}`
- `GET /runtime/contracts/commands/{commandKey}/versions/{versionNumber}`

Event responses return one artifact. Command responses return both sides of the command version:

```json
{
  "commandKey": "inventories.reserve",
  "version": "1.0.0",
  "requestArtifact": {
    "artifactType": "CommandRequest",
    "payloadSchema": {}
  },
  "replyArtifact": {
    "artifactType": "CommandReply",
    "payloadSchema": {}
  }
}
```

`replyArtifact` is optional. Request artifacts are required.

## Local Development

Build and test:

```powershell
dotnet restore KnOwl.slnx
dotnet build KnOwl.slnx --no-restore --configuration Release
dotnet test KnOwl.slnx --no-build --configuration Release
```

Pack the libraries:

```powershell
Get-ChildItem src -Recurse -Filter *.csproj | ForEach-Object {
  dotnet pack $_.FullName --configuration Release -o artifacts/packages
}
```

Run the distribution end-to-end test. This starts SQL Server in Docker, runs the Control Plane and Runtime samples, and validates SQL Server storage plus push/pull artifact distribution for both target frameworks.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-knowl-distribution-e2e.ps1
```

## Release

The release workflow builds, tests, packs, creates the GitHub release, and publishes NuGet packages. Production releases are driven by `.release` and `CHANGELOG.md`.

Current release: `1.0.0`
