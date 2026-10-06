using Google.Protobuf;
using Grpc.Core;
using KnOwl.Runtime.Bootstrap.Grpc;

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
}
