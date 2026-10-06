using Google.Protobuf;
using Grpc.Core;
using KnOwl.Contracts.Artifacts;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Bootstrap.Grpc;
using KnOwl.Runtime.Core;

namespace KnOwl.Tests;

public sealed class GeneratedGrpcCoverageTests
{
    [Fact]
    public void RuntimeContractMessagesRoundTripThroughGeneratedProtobufCode()
    {
        var exact = new RuntimeContractExactRequest
        {
            ArtifactType = "Command",
            Topic = "orders.submit",
            VersionNumber = "1.2.3"
        };

        var latest = new RuntimeContractLatestRequest
        {
            ArtifactType = "Event",
            Topic = "orders.submitted"
        };

        var response = new RuntimeContractResponse
        {
            Found = true,
            Id = Guid.NewGuid().ToString("N"),
            SourceArtifactId = Guid.NewGuid().ToString("N"),
            SourceReleaseId = Guid.NewGuid().ToString("N"),
            ArtifactType = "Command",
            DefinitionId = Guid.NewGuid().ToString("N"),
            VersionId = Guid.NewGuid().ToString("N"),
            Name = "Submit Order",
            Topic = "orders.submit",
            VersionNumber = "1.2.3",
            Description = "Submit an order.",
            PayloadSchemaJson = "{\"type\":\"object\"}",
            ContentHash = "hash",
            DeployedAtUtc = DateTimeOffset.UtcNow.ToString("O")
        };

        AssertMessageRoundTrips(exact, RuntimeContractExactRequest.Parser);
        AssertMessageRoundTrips(latest, RuntimeContractLatestRequest.Parser);
        AssertMessageRoundTrips(response, RuntimeContractResponse.Parser);

        Assert.False(exact.Equals(new RuntimeContractExactRequest(exact) { VersionNumber = "9.9.9" }));
        Assert.False(latest.Equals(new RuntimeContractLatestRequest(latest) { Topic = "changed" }));
        Assert.False(response.Equals(new RuntimeContractResponse(response) { Found = false }));
        Assert.NotNull(KnowlRuntimeContractsReflection.Descriptor);
        Assert.NotNull(RuntimeContracts.Descriptor);
    }

    [Fact]
    public void RuntimeContractGeneratedMessagesCoverMergeNullSettersAndUnknownFields()
    {
        var exact = new RuntimeContractExactRequest
        {
            ArtifactType = "Command",
            Topic = "orders.submit",
            VersionNumber = "1.2.3"
        };
        var latest = new RuntimeContractLatestRequest
        {
            ArtifactType = "Event",
            Topic = "orders.submitted"
        };
        var response = new RuntimeContractResponse
        {
            Found = true,
            Id = "id",
            SourceArtifactId = "source-artifact",
            SourceReleaseId = "source-release",
            ArtifactType = "Command",
            DefinitionId = "definition",
            VersionId = "version",
            Name = "Submit",
            Topic = "orders.submit",
            VersionNumber = "1.2.3",
            Description = "Description",
            PayloadSchemaJson = "{}",
            ContentHash = "hash",
            DeployedAtUtc = "2026-09-18T20:07:00Z"
        };

        AssertGeneratedMessageBranches(exact, clone =>
        {
            clone.ArtifactType = string.Empty;
            clone.Topic = string.Empty;
            clone.VersionNumber = string.Empty;
        });
        AssertGeneratedMessageBranches(latest, clone =>
        {
            clone.ArtifactType = string.Empty;
            clone.Topic = string.Empty;
        });
        AssertGeneratedMessageBranches(response, clone =>
        {
            clone.Found = false;
            clone.Id = string.Empty;
            clone.SourceArtifactId = string.Empty;
            clone.SourceReleaseId = string.Empty;
            clone.ArtifactType = string.Empty;
            clone.DefinitionId = string.Empty;
            clone.VersionId = string.Empty;
            clone.Name = string.Empty;
            clone.Topic = string.Empty;
            clone.VersionNumber = string.Empty;
            clone.Description = string.Empty;
            clone.PayloadSchemaJson = string.Empty;
            clone.ContentHash = string.Empty;
            clone.DeployedAtUtc = string.Empty;
        });

        var exactUnknown = RuntimeContractExactRequest.Parser.ParseFrom(CreateUnknownFieldBytes());
        var latestUnknown = RuntimeContractLatestRequest.Parser.ParseFrom(CreateUnknownFieldBytes());
        var responseUnknown = RuntimeContractResponse.Parser.ParseFrom(CreateUnknownFieldBytes());
        var mergedExact = new RuntimeContractExactRequest();
        var mergedLatest = new RuntimeContractLatestRequest();
        var mergedResponse = new RuntimeContractResponse();

        mergedExact.MergeFrom(exactUnknown);
        mergedLatest.MergeFrom(latestUnknown);
        mergedResponse.MergeFrom(responseUnknown);

        Assert.True(mergedExact.CalculateSize() > 0);
        Assert.True(mergedLatest.CalculateSize() > 0);
        Assert.True(mergedResponse.CalculateSize() > 0);
        Assert.NotEmpty(mergedExact.ToByteArray());
        Assert.NotEmpty(mergedLatest.ToByteArray());
        Assert.NotEmpty(mergedResponse.ToByteArray());
        Assert.Throws<ArgumentNullException>(() => exact.ArtifactType = null!);
        Assert.Throws<ArgumentNullException>(() => latest.Topic = null!);
        Assert.Throws<ArgumentNullException>(() => response.DeployedAtUtc = null!);
    }

    [Fact]
    public void RuntimeContractsGeneratedGrpcBindingsRegisterMethods()
    {
        var implementation = new TestRuntimeContracts();
        var definition = RuntimeContracts.BindService(implementation);
        var binder = new CapturingServiceBinder();

        RuntimeContracts.BindService(binder, implementation);
        RuntimeContracts.BindService(binder, null!);

        Assert.NotNull(definition);
        Assert.Equal(4, binder.BoundMethods);
    }

    [Fact]
    public async Task RuntimeContractsBaseThrowsForUnimplementedMethods()
    {
        var service = new UnimplementedRuntimeContracts();

        await Assert.ThrowsAsync<RpcException>(() => service.GetExact(new RuntimeContractExactRequest(), null!));
        await Assert.ThrowsAsync<RpcException>(() => service.GetLatest(new RuntimeContractLatestRequest(), null!));
    }

    [Fact]
    public async Task RuntimeContractsGrpcServiceMapsCatalogResultsAndInvalidArguments()
    {
        var artifact = new RuntimeContractArtifact
        {
            Id = Guid.NewGuid(),
            SourceArtifactId = Guid.NewGuid(),
            SourceReleaseId = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Command,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = "Submit Order",
            Topic = "orders.submit",
            VersionNumber = "1.2.3",
            Description = null,
            PayloadSchemaJson = """{"type":"object"}""",
            ContentHash = "hash",
            DeployedAtUtc = new DateTime(2026, 9, 18, 20, 7, 0, DateTimeKind.Local)
        };
        var catalog = new CatalogServiceFake(artifact);
        var service = new RuntimeContractsGrpcService(catalog);
        var context = new TestServerCallContext(CancellationToken.None);

        var exact = await service.GetExact(new RuntimeContractExactRequest
        {
            ArtifactType = "command",
            Topic = artifact.Topic,
            VersionNumber = artifact.VersionNumber
        }, context);
        var missing = await service.GetLatest(new RuntimeContractLatestRequest
        {
            ArtifactType = "event",
            Topic = "missing"
        }, context);

        Assert.True(exact.Found);
        Assert.Equal(artifact.Id.ToString("N"), exact.Id);
        Assert.Equal("Command", exact.ArtifactType);
        Assert.Equal(string.Empty, exact.Description);
        Assert.Contains("2026", exact.DeployedAtUtc, StringComparison.Ordinal);
        Assert.False(missing.Found);

        var exactError = await Assert.ThrowsAsync<RpcException>(() => service.GetExact(new RuntimeContractExactRequest { ArtifactType = "bad" }, context));
        var latestError = await Assert.ThrowsAsync<RpcException>(() => service.GetLatest(new RuntimeContractLatestRequest { ArtifactType = "bad" }, context));
        Assert.Equal(StatusCode.InvalidArgument, exactError.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, latestError.StatusCode);
    }

    private static void AssertMessageRoundTrips<TMessage>(TMessage message, MessageParser<TMessage> parser)
        where TMessage : class, IMessage<TMessage>, new()
    {
        var bytes = message.ToByteArray();
        var parsed = parser.ParseFrom(bytes);
        var clone = parsed.Clone();
        var merged = new TMessage();

        merged.MergeFrom(bytes);

        Assert.True(parsed.Equals(clone));
        Assert.True(parsed.GetHashCode() != 0 || parsed.CalculateSize() == 0);
        Assert.Equal(parsed.ToString(), clone.ToString());
        Assert.Equal(parsed.CalculateSize(), merged.CalculateSize());
    }

    private static void AssertGeneratedMessageBranches<TMessage>(TMessage message, Action<TMessage> clear)
        where TMessage : class, IMessage<TMessage>, IEquatable<TMessage>, new()
    {
        var merged = new TMessage();
        var empty = new TMessage();

        merged.MergeFrom(message);
        merged.MergeFrom((TMessage)null!);
        clear(empty);

        Assert.True(message.Equals((object)message));
        Assert.False(message.Equals((object)"different"));
        Assert.False(message.Equals((TMessage?)null));
        Assert.True(merged.CalculateSize() >= message.CalculateSize());
        Assert.Equal(0, empty.CalculateSize());
    }

    private static byte[] CreateUnknownFieldBytes()
    {
        using MemoryStream stream = new();
        using (CodedOutputStream output = new(stream, leaveOpen: true))
        {
            output.WriteTag(100, WireFormat.WireType.Varint);
            output.WriteInt32(7);
        }

        return stream.ToArray();
    }

    private sealed class TestRuntimeContracts : RuntimeContracts.RuntimeContractsBase
    {
        public override Task<RuntimeContractResponse> GetExact(RuntimeContractExactRequest request, ServerCallContext context)
        {
            return Task.FromResult(new RuntimeContractResponse
            {
                Found = true,
                ArtifactType = request.ArtifactType,
                Topic = request.Topic,
                VersionNumber = request.VersionNumber
            });
        }

        public override Task<RuntimeContractResponse> GetLatest(RuntimeContractLatestRequest request, ServerCallContext context)
        {
            return Task.FromResult(new RuntimeContractResponse
            {
                Found = true,
                ArtifactType = request.ArtifactType,
                Topic = request.Topic
            });
        }
    }

    private sealed class UnimplementedRuntimeContracts : RuntimeContracts.RuntimeContractsBase
    {
    }

    private sealed class CapturingServiceBinder : ServiceBinderBase
    {
        public int BoundMethods { get; private set; }

        public override void AddMethod<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            UnaryServerMethod<TRequest, TResponse>? handler)
        {
            BoundMethods++;
        }
    }

    private sealed class CatalogServiceFake(RuntimeContractArtifact artifact) : IRuntimeContractCatalogService
    {
        public Task<IReadOnlyList<RuntimeContractArtifact>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RuntimeContractArtifact>>([artifact]);

        public Task<RuntimeContractArtifact?> GetExact(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<RuntimeContractArtifact?>(artifact.ArtifactType == artifactType && artifact.Topic == topic && artifact.VersionNumber == versionNumber ? artifact : null);

        public Task<RuntimeContractArtifact?> GetLatest(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken = default)
            => Task.FromResult<RuntimeContractArtifact?>(artifact.ArtifactType == artifactType && artifact.Topic == topic ? artifact : null);

        public Task<RuntimeContractArtifact?> GetEvent(string eventKey, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<RuntimeContractArtifact?>(null);

        public Task<CommandContractArtifacts<RuntimeContractArtifact>?> GetCommand(string commandKey, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<CommandContractArtifacts<RuntimeContractArtifact>?>(null);
    }

    private sealed class TestServerCallContext(CancellationToken cancellationToken) : ServerCallContext
    {
        private readonly Metadata responseTrailers = [];
        private Status status;
        private WriteOptions? writeOptions;

        protected override string MethodCore => "GetExact";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "ipv4:127.0.0.1:12345";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore { get; } = [];
        protected override CancellationToken CancellationTokenCore => cancellationToken;
        protected override Metadata ResponseTrailersCore => responseTrailers;
        protected override Status StatusCore { get => status; set => status = value; }
        protected override WriteOptions? WriteOptionsCore { get => writeOptions; set => writeOptions = value; }
        protected override AuthContext AuthContextCore { get; } = new("anonymous", []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
            => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
            => Task.CompletedTask;
    }
}
