# KnOwl

[![Build](https://github.com/mape1402/knowl/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/mape1402/knowl/actions/workflows/build-and-release.yml)
[![Package](https://img.shields.io/nuget/v/KnOwl.Contracts.svg?label=package)](https://www.nuget.org/packages/KnOwl.Contracts)
[![Downloads](https://img.shields.io/nuget/dt/KnOwl.Contracts.svg?label=downloads)](https://www.nuget.org/packages/KnOwl.Contracts)
[![Coverage](https://img.shields.io/badge/coverage-99.06%25-brightgreen.svg)](tests/KnOwl.Tests/KnOwl.Tests.csproj)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/mape1402/knowl)

<img src="assets/knowl-readme-hero.png" alt="KnOwl contract design and runtime distribution" width="100%">

KnOwl is a set of reusable .NET libraries for designing, versioning, promoting, distributing, and consuming async contract metadata. It gives teams a Control Plane where contracts are authored and released, plus a Runtime surface where deployed contracts can be consumed by running services.

The goal is to keep product hosts thin. Your app owns the executable project, configuration, and EF migrations; KnOwl packages provide the domain model, application services, storage adapters, Razor UI, catalog endpoints, distribution endpoints, and runtime synchronization behavior.

## What KnOwl Solves

- Design event and command contracts with versioned payload schemas.
- Model commands with a required request schema and optional reply schema.
- Promote versions through lifecycle states and generate immutable artifacts.
- Distribute deployed artifacts from a Control Plane to one or more Runtime hosts.
- Expose deployed schemas through split event and command catalog endpoints.
- Navigate reusable Web UI screens with card-based browsing, searchable operational lists, and version details.
- Browse Markdown documentation spaces, topics, pages, and page versions from the Control Plane UI.
- Render Documentation Markdown with a navigable page index, in-page search, and Mermaid diagrams with zoom, pan, and expanded-view controls.
- Theme Control Plane and Runtime hosts with light and dark palettes, custom titles, icons, and sidebar branding.
- Keep host-specific database migrations outside the reusable NuGet libraries.

## Packages

Install only the layer your host needs:

| Package | Purpose |
| --- | --- |
| `KnOwl.Contracts` | Shared DTOs for artifacts, delivery, catalog responses, and distribution credential primitives. |
| `KnOwl.ControlPlane` | Control Plane domain model and repository contracts. |
| `KnOwl.ControlPlane.Application` | Control Plane services for design, lifecycle, artifacts, releases, and delivery. |
| `KnOwl.ControlPlane.Api` | Minimal API endpoints for Control Plane automation and external integrations. |
| `KnOwl.ControlPlane.Storage.EntityFramework` | Provider-agnostic EF Core storage for Control Plane state. |
| `KnOwl.ControlPlane.WebUI` | Reusable Razor UI for Control Plane hosts. |
| `KnOwl.ControlPlane.Bootstrap` | ASP.NET Core composition for Control Plane hosts. |
| `KnOwl.Runtime` | Runtime domain model and repository contracts. |
| `KnOwl.Runtime.Application` | Runtime catalog, deployment, pull, and connection credential services. |
| `KnOwl.Runtime.Api` | Minimal API endpoints for Runtime administration and artifact consumption. |
| `KnOwl.Runtime.Storage.EntityFramework` | Provider-agnostic EF Core storage for Runtime state. |
| `KnOwl.Runtime.WebUI` | Reusable Razor UI for Runtime hosts. |
| `KnOwl.Runtime.Bootstrap` | ASP.NET Core composition for Runtime hosts. |
| `KnOwl.WolfAuth` | Optional WolfAuth authentication integration and login shell for KnOwl hosts. |

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

    options.Theme.Title = "My Contracts";
    options.Theme.Mode = KnOwl.ControlPlane.WebUI.KnOwlThemeMode.Dark;
    options.Theme.IconImageUrl = "/img/company-icon.png";

    options.Theme.Light.PrimaryColor = "#2563eb";
    options.Theme.Light.PrimaryHoverColor = "#1d4ed8";
    options.Theme.Light.SidebarBackgroundColor = "#0f2a44";
    options.Theme.Light.SidebarBrandBackgroundColor = "#0b1f33";
    options.Theme.Light.SidebarTextColor = "#ffffff";
    options.Theme.Light.SidebarMutedTextColor = "#bfdbfe";

    options.Theme.Dark.PrimaryColor = "#60a5fa";
    options.Theme.Dark.PrimaryHoverColor = "#93c5fd";
    options.Theme.Dark.SidebarBackgroundColor = "#0f172a";
    options.Theme.Dark.SidebarBrandBackgroundColor = "#0b1220";
    options.Theme.Dark.SidebarTextColor = "#f8fafc";
    options.Theme.Dark.SidebarMutedTextColor = "#bfdbfe";
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

dotnet ef database update --context KnOwlDbContext
```

Run the host and open the Control Plane UI. From there you can create data types, custom metadata fields, events, commands, versions, artifacts, runtime environments, runtime nodes, and releases.
Control Plane list and detail screens include query-string search for contracts, versions, artifacts, releases, environments, and runtime node setup, so teams can filter large catalogs without custom host code.
Documentation pages include rendered-text search with highlighted matches and previous/next navigation. Mermaid diagrams render directly from fenced Markdown blocks and include zoom out, reset, zoom in, and expanded-view controls without requiring host-specific JavaScript.

The bootstrap package also maps the Control Plane REST API at `/api/v1/control-plane`.

Theme configuration is optional. When omitted, the reusable Web UI uses KnOwl's default purple and white light palette plus its default dark palette. Hosts can override the title, icon, initial mode, and separate light/dark colors from `AddKnOwlControlPlane` without changing package assets.
The Control Plane UI uses the same card-based navigation patterns across contracts, distribution, documentation, and administrative views so hosts get a complete management experience without rebuilding screens.

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

    options.Theme.Title = "My Runtime";
    options.Theme.Subtitle = "Contract cache";
    options.Theme.Mode = KnOwl.Runtime.WebUI.KnOwlThemeMode.Dark;
    options.Theme.IconImageUrl = "/img/company-icon.png";

    options.Theme.Light.PrimaryColor = "#2563eb";
    options.Theme.Light.PrimaryHoverColor = "#1d4ed8";
    options.Theme.Light.SidebarBackgroundColor = "#0f2a44";
    options.Theme.Light.SidebarBrandBackgroundColor = "#0b1f33";
    options.Theme.Light.SidebarTextColor = "#ffffff";
    options.Theme.Light.SidebarMutedTextColor = "#bfdbfe";

    options.Theme.Dark.PrimaryColor = "#60a5fa";
    options.Theme.Dark.PrimaryHoverColor = "#93c5fd";
    options.Theme.Dark.SidebarBackgroundColor = "#0f172a";
    options.Theme.Dark.SidebarBrandBackgroundColor = "#0b1220";
    options.Theme.Dark.SidebarTextColor = "#f8fafc";
    options.Theme.Dark.SidebarMutedTextColor = "#bfdbfe";
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

dotnet ef database update --context KnOwlRuntimeDbContext
```

The bootstrap package also maps the Runtime REST API at `/api/v1/runtime`.

Runtime theme configuration is optional and follows the same host-owned pattern as the Control Plane. Hosts can set the sidebar title, subtitle, icon, initial mode, and separate light/dark palettes directly on `options.Theme`.
If `SidebarBrandBackgroundColor` is not set in a palette, the Runtime sidebar header falls back to that palette's `SidebarBackgroundColor` so the host theme remains consistent.
The Runtime UI includes Control Plane-aligned navigation, searchable Control Plane connections, and artifact catalog filters for deployed contract caches.

### 3. Connect Runtime to Control Plane

1. In the Runtime UI, create a Control Plane connection.
2. Generate or import the connection credentials.
3. In the Control Plane UI, register the Runtime node and credentials.
4. Release deployed artifacts from the Control Plane.
5. Let the Runtime pull pending artifacts, or push artifacts to the Runtime delivery endpoint.

The sample hosts show the intended shape:

- `samples/KnOwl.ControlPlaneHost.Sample`
- `samples/KnOwl.RuntimeHost.Sample`

The sample design-time DbContext factories target the same sample databases used at runtime. This keeps `dotnet ef database update` aligned with the hosts you run locally.

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

Hosts remain responsible for authentication. The API packages do not force JWT, cookies, managed identity, or API-key infrastructure.

## Authentication With WolfAuth

KnOwl delegates user authentication to WolfAuth. The host configures WolfAuth directly, including its provider, schemes, claims, and storage. KnOwl only adds the reusable login shell and middleware hooks needed by the Control Plane or Runtime UI.

WolfAuth integration is enabled by default when a host calls `UseWolfAuth`. Disable it per environment with `KnOwl:WolfAuth:Enabled = false`; when disabled, KnOwl skips the login gate and hides the top-bar user menu.

Typical Control Plane setup:

```csharp
using KnOwl.ControlPlane.Bootstrap;
using KnOwl.WolfAuth;
using Microsoft.EntityFrameworkCore;
using WolfAuth.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWolfAuth(wolf =>
{
    // Configure WolfAuth directly here: provider, claims mapping, persistence, and policies.
});

builder.Services
    .AddKnOwlControlPlane(builder.Configuration, options =>
    {
        options.MigrationsAssembly = typeof(Program).Assembly.GetName().Name;
        options.ConfigureStorage = db => db.UseSqlServer(
            builder.Configuration.GetConnectionString("KnOwlDb"),
            sql => sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name));
    })
    .UseWolfAuth(builder.Configuration.GetSection("KnOwl:WolfAuth"), options =>
    {
        options.ApplicationName = "My Contracts";
        options.Subtitle = "Sign in to continue.";
        options.LoginButtonText = "Login";
    });

var app = builder.Build();

app.MapKnOwlControlPlane();

app.Run();
```

Configuration:

```json
{
  "KnOwl": {
    "WolfAuth": {
      "Enabled": true,
      "LoginPath": "/auth/login",
      "ChallengePath": "/auth/login/challenge",
      "LogoutPath": "/auth/logout"
    }
  }
}
```

When `UseWolfAuth` is enabled, anonymous browser requests are redirected to `/auth/login`. That page renders a single `Login` button that triggers `/auth/login/challenge`, and the challenge uses the authentication scheme configured by the host through WolfAuth/ASP.NET Core authentication.

This phase covers authentication. KnOwl's previous in-package user security layer for subjects, roles, permissions, authentication, and security storage is deprecated in favor of WolfAuth as the central security layer. Distribution credential/token primitives used for Control Plane and Runtime artifact delivery remain part of KnOwl because they secure node-to-node distribution, not user sign-in.

## Local Development

Build and test:

```powershell
dotnet restore KnOwl.slnx
dotnet build KnOwl.slnx --no-restore --configuration Release
dotnet test KnOwl.slnx --no-build --configuration Release
```

The test suite is intentionally granular. It includes focused unit coverage for public contract shapes, API mappers, token providers, page models, WolfAuth integration, distribution credential security, storage repositories, distribution flows, Documentation rendering, and Runtime behavior. The current release validates 3,164 xUnit cases on `net9.0` and 2,932 on `net10.0` with 99%+ line coverage.

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

Current release: `2.3.0`
