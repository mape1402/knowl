# KnOwl

[![Build](https://github.com/mape1402/knowl/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/mape1402/knowl/actions/workflows/build-and-release.yml)
[![NuGet](https://img.shields.io/nuget/v/KnOwl.svg)](https://www.nuget.org/packages/KnOwl)
[![Downloads](https://img.shields.io/nuget/dt/KnOwl.svg)](https://www.nuget.org/packages/KnOwl)
[![License](https://img.shields.io/github/license/mape1402/knowl.svg)](LICENSE)

KnOwl is the Elysium inbox/outbox toolkit for .NET services.

It protects incoming work from duplicate execution with the inbox pattern, persists outgoing work with the outbox pattern, and uses Mule durable actions for deferred execution. The core is transport-neutral: HTTP, messaging, Pigeon, custom transports, and dashboard diagnostics all use the same model.

## Packages

```bash
dotnet add package KnOwl
dotnet add package KnOwl.InMemory
dotnet add package KnOwl.EntityFrameworkCore
dotnet add package KnOwl.AspNetCore
dotnet add package KnOwl.AspNetCore.Dashboard
dotnet add package KnOwl.Messaging
dotnet add package KnOwl.Messaging.Pigeon
dotnet add package KnOwl.Mule
```

Package reference example:

```xml
<PackageReference Include="KnOwl" Version="2.1.0" />
<PackageReference Include="KnOwl.AspNetCore" Version="2.1.0" />
<PackageReference Include="KnOwl.AspNetCore.Dashboard" Version="2.1.0" />
<PackageReference Include="KnOwl.EntityFrameworkCore" Version="2.1.0" />
<PackageReference Include="KnOwl.Mule" Version="2.1.0" />
```

## Getting Started

Register core services, choose storage, and add Mule when you want durable deferred execution:

```csharp
using Mule.InMemory;
using KnOwl;
using KnOwl.EntityFrameworkCore;
using KnOwl.Mule;

services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

services
    .AddKnOwl(options =>
    {
        options.DefaultEntryLifetime = TimeSpan.FromHours(24);
        options.AllowPayloadHashAsIdempotencyKey = true;
        options.ScanAssemblyContaining<OrdersFingerprintProfile>();
    })
    .UseEntityFramework<AppDbContext>();

services.AddKnOwlMule();
services.AddMule(mule => mule
    .UseInMemory()
    .AddActionsFromAssemblyContaining<KnOwlOutboxMuleAction>());
```

KnOwl augments the registered `AppDbContext` automatically. Your application `DbContext`
does not need KnOwl `DbSet` properties or `OnModelCreating` changes.

Use `UseInMemory()` instead of EF for tests, samples, and local development.

## Inbox

The inbox pattern identifies incoming work by:

```text
Source + Operation + IdempotencyKey
```

Basic usage:

```csharp
var open = await inbox.OpenOrContinueAsync(
    InboxOpenRequest.For(
        source: "http",
        operation: "POST /orders",
        idempotencyKey: "client-key-1",
        payload: request),
    cancellationToken);

if (!open.Accepted)
    return;

try
{
    await handler.Handle(request, cancellationToken);
    await inbox.CompleteCurrentAsync(cancellationToken: cancellationToken);
}
catch (Exception ex)
{
    await inbox.FailCurrentAsync(ex, cancellationToken);
    throw;
}
```

Entries use ULID ids and move through `Started`, `Completed`, `Failed`, and `Expired`.

## Declared Operations

Declared operations are the durable-safe path for switching HTTP or other synchronous entry points between inline and deferred execution.

```csharp
public sealed class CreateOrderOperation
    : KnOwlOperation<CreateOrderRequest, OrderCreated>
{
    protected override async ValueTask<OrderCreated> ExecuteAsync(
        CreateOrderRequest request,
        KnOwlOperationContext context,
        CancellationToken cancellationToken)
    {
        var service = context.Services.GetRequiredService<IOrderService>();
        return await service.CreateAsync(request, cancellationToken);
    }
}
```

Invoke from an endpoint:

```csharp
var result = await operations
    .ExecuteAsync<CreateOrderOperation, CreateOrderRequest, OrderCreated>(
        request,
        cancellationToken);

if (result.Deferred)
    return Results.Accepted($"/inbox/{result.Context.Entry.Id}");

if (result.Executed)
    return Results.Created($"/orders/{result.Result.Id}", result.Result);

return Results.Conflict(result.Decision);
```

When execution is deferred, KnOwl stores the inbox entry first, schedules a Mule durable action, and completes/fails the inbox later from a worker scope.

## Outbox

The outbox pattern persists outgoing work before it is delivered by a transport.

```csharp
var envelope = await outbox.EnqueueAsync(new OutboxEnqueueRequest
{
    Transport = "pigeon",
    Operation = "orders.created",
    Destination = "orders",
    Payload = new OrderCreated(orderId),
    CorrelationId = correlationId
}, cancellationToken);
```

KnOwl stores an `OutboxEnvelope` with:

```text
Ulid Id
Transport
Operation
Destination
PayloadType
Payload
Headers
Metadata
CorrelationId
Status
```

Mule later executes `knowl.outbox.publish.v1`, loads the envelope by ULID, resolves the matching `IOutboxTransportPublisher`, and marks the envelope as `Published` or `Failed`.

Custom publisher:

```csharp
public sealed class MyPublisher : IOutboxTransportPublisher
{
    public string Transport => "my-transport";

    public async ValueTask<OutboxPublishResult> PublishAsync(
        OutboxEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        await client.SendAsync(envelope.Payload, cancellationToken);
        return OutboxPublishResult.Success;
    }
}
```

## Profiles

Inbox fingerprints and outbox profiles are discovered from scanned assemblies.

```csharp
public sealed class OrdersFingerprintProfile : InboxFingerprintProfile
{
    public override void Configure(InboxFingerprintProfileBuilder builder)
    {
        builder.For<CreateOrderRequest>()
            .Use(request => new
            {
                request.CustomerId,
                request.ExternalOrderId,
                request.Amount
            });
    }
}
```

Outbox profiles can customize how enqueue requests become durable envelopes without adding noisy fluent configuration.

## ASP.NET Core

Add HTTP idempotency:

```csharp
using KnOwl.AspNetCore;

services.AddKnOwlAspNetCore(options =>
{
    options.RequestHeaderNames.Clear();
    options.RequestHeaderNames.Add("Idempotency-Key");
    options.ResponseHeaderName = "Idempotency-Key";
    options.CaptureCompletedResponses = true;
    options.ReplayCompletedResponses = true;
});

app.UseKnOwl();
```

If a request has an idempotency header, middleware reserves the inbox entry before the endpoint runs. If the request does not have a header, your endpoint can open KnOwl after model binding so the computed key is based on the DTO instead of raw body bytes.

## Dashboard

Add the event-driven dashboard:

```csharp
using KnOwl.AspNetCore.Dashboard;

services.AddKnOwlDashboard(options =>
{
    options.Authentication.RootUser.Username = "admin";
    options.Authentication.RootUser.Password = "<from-secret-store>";
});

app.MapKnOwlDashboard("/knowl");
```

The dashboard:

- Loads initial Inbox/Outbox history from storage.
- Receives live events through Server-Sent Events.
- Does not poll the database.
- Shows Inbox, Outbox, Deferred Work, and live Events.
- Uses the KnOwl logo palette.
- Is closed by default: root user, ASP.NET Core auth, or custom auth must be configured.

ASP.NET Core auth mode:

```csharp
services.AddKnOwlDashboard(options =>
{
    options.Authentication.Mode = KnOwlDashboardAuthenticationMode.AspNetCoreAuthentication;
});

app.MapKnOwlDashboard("/knowl")
   .RequireAuthorization("KnOwlDashboard");
```

Custom auth mode:

```csharp
services.AddSingleton<IKnOwlDashboardAuthenticator, MyDashboardAuthenticator>();
services.AddKnOwlDashboard(options =>
{
    options.Authentication.Mode = KnOwlDashboardAuthenticationMode.Custom;
});
```

## Messaging

Use `KnOwl.Messaging` when building a transport adapter or orchestration layer:

```csharp
var result = await messages.OpenAsync(new InboxMessageContext
{
    Transport = "rabbitmq",
    Topic = "orders",
    Version = "1.0.0",
    Subscription = "billing",
    Operation = "created",
    MessageId = messageId,
    Payload = payload,
    Metadata = metadata
});

if (result.ShouldExecute)
{
    await consumer.Handle(payload, cancellationToken);
    await inbox.CompleteCurrentAsync(cancellationToken: cancellationToken);
}

messages.AttachEffectiveKey(replyMetadata);
```

The default operation shape is:

```text
topic:version/subscription/operation
```

## Pigeon

`KnOwl.Messaging.Pigeon` targets Pigeon 4.0.0 and integrates with consume and publish interceptors.

```csharp
services.AddKnOwlPigeon(options =>
{
    options.Transport = "pigeon";
    options.EnableOutbox = true;
    options.ExecutionModeResolver = context =>
        context.Topic == "orders.deferred"
            ? InboxExecutionMode.Deferred
            : InboxExecutionMode.Inline;
});
```

Consume:

- Decision interceptor opens the inbox before the consumer handler.
- Execution interceptor completes or fails the inbox after the handler.
- Deferred replay uses `IPigeonConsumerInvoker`.

Publish:

- Publish decision interceptor persists Pigeon's prepared `PigeonPublishEnvelope` in KnOwl Outbox.
- Pigeon publish is skipped inline after the envelope is durable.
- Mule later publishes through `IPigeonPublisherInvoker` without rerunning producer interceptors, publish decision interceptors, or Pigeon's internal outbox logic.
- Normal and raw publish flows are supported.

## Mule

Register KnOwl actions with Mule:

```csharp
services.AddKnOwlMule();
services.AddMule(mule => mule
    .UseInMemory()
    .AddActionsFromAssemblyContaining<KnOwlOutboxMuleAction>()
    .AddActionsFromAssemblyContaining<KnOwlPigeonMuleAction>());
```

KnOwl attaches durable metadata such as `knowl-inbox-id`, `knowl-outbox-id`, and `idempotency-key`.

## Sample

Run the sample app:

```bash
dotnet run --project samples/KnOwl.Sample/KnOwl.Sample.csproj --urls http://127.0.0.1:5188
```

Try:

```bash
curl -i -X POST http://127.0.0.1:5188/orders/inline \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: inline-key-1" \
  -d "{\"customerId\":\"cust-1\",\"externalOrderId\":\"inline-1\",\"amount\":42.5}"

curl -i -X POST http://127.0.0.1:5188/orders/deferred \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: deferred-key-1" \
  -d "{\"customerId\":\"cust-3\",\"externalOrderId\":\"deferred-1\",\"amount\":99.99}"

curl -i -X POST http://127.0.0.1:5188/pigeon/inline \
  -H "Content-Type: application/json" \
  -d "{\"orderId\":\"pigeon-inline-1\",\"customerId\":\"cust-4\",\"amount\":12.50}"

curl -i -X POST http://127.0.0.1:5188/outbox/direct \
  -H "Content-Type: application/json" \
  -d "{\"orderId\":\"audit-1\",\"reason\":\"manual-check\"}"
```

Open the dashboard at `http://127.0.0.1:5188/knowl` with `admin / secret`.

## Testing

The test suite covers:

- Core inbox lifecycle, policies, fingerprints, operation execution, and outbox publication.
- In-memory inbox/outbox storage.
- ASP.NET Core idempotency and dashboard auth/state.
- Messaging and Pigeon consume/publish adapters.
- Mule deferred inbox and outbox execution.
- EF Core SQL Server e2e tests using Docker.

Run everything:

```bash
dotnet test KnOwl.slnx -c Debug
```
