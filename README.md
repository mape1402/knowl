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
| `KnOwl.ControlPlane.Api` | Minimal API endpoints for Control Plane automation and external integrations. |
| `KnOwl.ControlPlane.Storage.EntityFramework` | Provider-agnostic EF Core storage for Control Plane state. |
| `KnOwl.ControlPlane.WebUI` | Reusable Razor UI for Control Plane hosts. |
| `KnOwl.ControlPlane.Bootstrap` | ASP.NET Core composition for Control Plane hosts. |
| `KnOwl.Runtime` | Runtime domain model and repository contracts. |
| `KnOwl.Runtime.Application` | Runtime catalog, deployment, pull, and security services. |
| `KnOwl.Runtime.Api` | Minimal API endpoints for Runtime administration and artifact consumption. |
| `KnOwl.Runtime.Storage.EntityFramework` | Provider-agnostic EF Core storage for Runtime state. |
| `KnOwl.Runtime.WebUI` | Reusable Razor UI for Runtime hosts. |
| `KnOwl.Runtime.Bootstrap` | ASP.NET Core composition for Runtime hosts. |
| `KnOwl.Security` | Provider-agnostic subject resolution, roles, permissions, and ASP.NET Core authorization policies. |
| `KnOwl.Security.Storage.EntityFramework` | Provider-agnostic EF Core storage for KnOwl subjects, role assignments, permission assignments, and external group mappings. |

All packages target `net9.0` and `net10.0`.

The `*.Storage.EntityFramework` and bootstrap packages depend on EF Core relational APIs, not on a concrete database provider. Hosts choose the provider by configuring the DbContext options, the same way they would configure EF Core directly.

## Getting Started

### 1. Create a Control Plane host

```powershell
dotnet new web -n MyCompany.Contracts.ControlPlane
cd MyCompany.Contracts.ControlPlane

dotnet add package KnOwl.ControlPlane.Bootstrap
dotnet add package KnOwl.ControlPlane.Storage.EntityFramework
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Use the bootstrap package in `Program.cs`:

```csharp
using KnOwl.ControlPlane.Bootstrap;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var migrationsAssembly = typeof(Program).Assembly.GetName().Name!;
var connectionString = builder.Configuration.GetConnectionString("KnOwlDb");

builder.Services.AddKnOwlControlPlane(builder.Configuration, options =>
{
    options.MigrationsAssembly = migrationsAssembly;
    options.ConfigureStorage = db => db.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly(migrationsAssembly));
});

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

dotnet ef migrations add InitialKnOwlSecurity `
  --context KnOwlSecurityDbContext `
  --output-dir Migrations/Security

dotnet ef database update --context KnOwlDbContext
dotnet ef database update --context KnOwlSecurityDbContext
```

Run the host and open the Control Plane UI. From there you can create data types, custom metadata fields, events, commands, versions, artifacts, runtime environments, runtime nodes, and releases.

The bootstrap package also maps the Control Plane REST API at `/api/v1/control-plane`.

To use a different EF Core provider, install that provider in the host and configure storage with that provider:

```csharp
using KnOwl.ControlPlane.Bootstrap;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKnOwlControlPlane(builder.Configuration, options =>
{
    options.ConfigureStorage = db => db.UseNpgsql(
        builder.Configuration.GetConnectionString("KnOwlDb"),
        provider => provider.MigrationsAssembly(typeof(Program).Assembly.GetName().Name));

    options.ConfigureSecurityStorage = db => db.UseNpgsql(
        builder.Configuration.GetConnectionString("KnOwlSecurityDb"),
        provider => provider.MigrationsAssembly(typeof(Program).Assembly.GetName().Name));
});
```

### 2. Create a Runtime host

```powershell
dotnet new web -n MyCompany.Contracts.Runtime
cd MyCompany.Contracts.Runtime

dotnet add package KnOwl.Runtime.Bootstrap
dotnet add package KnOwl.Runtime.Storage.EntityFramework
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Use the runtime bootstrap in `Program.cs`:

```csharp
using KnOwl.Runtime.Bootstrap;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var migrationsAssembly = typeof(Program).Assembly.GetName().Name!;
var connectionString = builder.Configuration.GetConnectionString("KnOwlRuntimeDb")
    ?? builder.Configuration.GetConnectionString("KnOwlDb");

builder.Services.AddKnOwlRuntime(builder.Configuration, options =>
{
    options.MigrationsAssembly = migrationsAssembly;
    options.ConfigureStorage = db => db.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly(migrationsAssembly));
});

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

dotnet ef migrations add InitialKnOwlSecurity `
  --context KnOwlSecurityDbContext `
  --output-dir Migrations/Security

dotnet ef database update --context KnOwlRuntimeDbContext
dotnet ef database update --context KnOwlSecurityDbContext
```

The bootstrap package also maps the Runtime REST API at `/api/v1/runtime`.

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

## REST API

KnOwl includes Minimal API packages for automation, CI tooling, portals, and custom hosts that need to drive KnOwl without using the Razor UI.

Control Plane hosts expose:

- `GET /api/v1/control-plane/schema-types`
- `POST /api/v1/control-plane/schema-types`
- `GET /api/v1/control-plane/metadata-fields`
- `POST /api/v1/control-plane/metadata-fields`
- `GET /api/v1/control-plane/events`
- `POST /api/v1/control-plane/events`
- `POST /api/v1/control-plane/events/{id}/versions`
- `POST /api/v1/control-plane/events/{id}/versions/{versionId}/transition`
- `GET /api/v1/control-plane/commands`
- `POST /api/v1/control-plane/commands`
- `POST /api/v1/control-plane/commands/{id}/versions`
- `POST /api/v1/control-plane/commands/{id}/versions/{versionId}/transition`
- `GET /api/v1/control-plane/artifacts`
- `POST /api/v1/control-plane/artifacts/events/{versionId}/build`
- `POST /api/v1/control-plane/artifacts/commands/{versionId}/build`
- `GET /api/v1/control-plane/runtime-environments`
- `GET /api/v1/control-plane/runtime-nodes`
- `POST /api/v1/control-plane/runtime-nodes/{id}/credentials/generate`
- `POST /api/v1/control-plane/runtime-nodes/{id}/credentials/import`
- `POST /api/v1/control-plane/runtime-nodes/{id}/connect/validate`
- `GET /api/v1/control-plane/releases`
- `POST /api/v1/control-plane/releases`
- `POST /api/v1/control-plane/releases/{id}/plan`
- `POST /api/v1/control-plane/releases/{id}/execute`

Runtime hosts expose:

- `GET /api/v1/runtime/status`
- `GET /api/v1/runtime/artifacts`
- `GET /api/v1/runtime/artifacts/events/{eventKey}/versions/{versionNumber}`
- `GET /api/v1/runtime/artifacts/commands/{commandKey}/versions/{versionNumber}`
- `GET /api/v1/runtime/control-planes`
- `POST /api/v1/runtime/control-planes`
- `POST /api/v1/runtime/control-planes/{id}/credentials/generate`
- `POST /api/v1/runtime/control-planes/{id}/credentials/import`
- `POST /api/v1/runtime/control-planes/{id}/connect/validate`
- `GET /api/v1/runtime/control-planes/{sourceKey}/artifacts/pending`
- `POST /api/v1/runtime/control-planes/{sourceKey}/artifacts/{releaseTargetId}/apply`

Creating a command through the API captures both schemas in the first version:

```json
POST /api/v1/control-plane/commands

{
  "name": "Reserve Inventory",
  "topic": "inventories.reserve",
  "description": "Reserve stock before checkout.",
  "versionNumber": "1.0.0",
  "requestDefinitionJson": "{\"type\":\"object\",\"properties\":{\"sku\":{\"type\":\"string\"}}}",
  "replyDefinitionJson": "{\"type\":\"object\",\"properties\":{\"accepted\":{\"type\":\"boolean\"}}}",
  "comment": "Initial command contract."
}
```

Security administration endpoints are also available on the Control Plane:

- `GET /api/v1/control-plane/security/subjects`
- `PUT /api/v1/control-plane/security/subjects`
- `GET /api/v1/control-plane/security/role-assignments`
- `POST /api/v1/control-plane/security/role-assignments`
- `GET /api/v1/control-plane/security/permission-assignments`
- `POST /api/v1/control-plane/security/permission-assignments`
- `GET /api/v1/control-plane/security/external-group-role-assignments`
- `POST /api/v1/control-plane/security/external-group-role-assignments`

Hosts remain responsible for authentication. The API packages do not force JWT, cookies, managed identity, or API-key infrastructure.

## Security

KnOwl keeps identity provider concerns in the host and keeps authorization rules in reusable libraries:

- The host authenticates users with Entra ID, cookies, OpenID Connect, JWT bearer tokens, or any other ASP.NET Core authentication handler.
- KnOwl resolves the authenticated principal into an external subject using configurable claims.
- KnOwl stores known subjects, direct roles, direct permissions, and external group-to-role mappings.
- KnOwl policies protect Web/API surfaces without depending on a specific identity provider.

Typical Control Plane setup:

```csharp
using KnOwl.ControlPlane.Bootstrap;
using KnOwl.Security.Authorization;
using KnOwl.Security.Subjects;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(/* host-owned scheme */)
    .AddJwtBearer(/* Entra ID, Auth0, local STS, etc. */);

builder.Services.AddKnOwlControlPlane(builder.Configuration, options =>
{
    options.MigrationsAssembly = typeof(Program).Assembly.GetName().Name;

    options.Security.RequireKnownSubject = true;
    options.Security.Subject.Provider = "entra-id";
    options.Security.BootstrapAdmins.Add(new KnOwlBootstrapSubject
    {
        Provider = "entra-id",
        SubjectId = "<external-user-object-id>"
    });

    options.Authorization.SecurityManagePolicy = KnOwlAuthorizationPolicies.SecurityManage;
    options.Authorization.CommandsWritePolicy = KnOwlAuthorizationPolicies.CommandsWrite;
    options.Authorization.EventsWritePolicy = KnOwlAuthorizationPolicies.EventsWrite;
    options.Authorization.ArtifactsBuildPolicy = KnOwlAuthorizationPolicies.ArtifactsBuild;
    options.Authorization.ReleasesExecutePolicy = KnOwlAuthorizationPolicies.ReleasesExecute;
});

var app = builder.Build();

app.MapKnOwlControlPlane();

app.Run();
```

`RequireKnownSubject` blocks authenticated users until they are registered in KnOwl. Bootstrap admins are the first-run and recovery mechanism: they are matched by provider and external subject id, receive admin access, and can be synchronized into KnOwl security storage.

Built-in roles:

- `Reader`
- `Designer`
- `ReleaseManager`
- `RuntimeOperator`
- `SecurityAdmin`
- `Admin`

External identity groups can be mapped to KnOwl roles, so Entra ID or another provider can remain the system of record for users while KnOwl remains the system of record for product-specific access.

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

Current release: `1.0.4`
