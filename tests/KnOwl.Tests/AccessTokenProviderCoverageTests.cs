using System.Net;
using System.Text;
using System.Text.Json;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Distribution;
using Microsoft.Extensions.Caching.Distributed;

namespace KnOwl.Tests;

public sealed class AccessTokenProviderCoverageTests
{
    [Fact]
    public async Task RuntimeAccessTokenProviderUsesCachedTokenWhenStillFresh()
    {
        var node = CreateRuntimeNode();
        var cache = new DictionaryDistributedCache();
        var scopes = new[] { ArtifactDeliveryScope.ArtifactPush };
        await cache.SetStringAsync(
            new ConnectionTokenCacheKeyBuilder().BuildConsumerTokenKey("control-plane", node.Id.ToString("N"), new DefaultConnectionScopeFormatter().FormatMany(scopes)),
            JsonSerializer.Serialize(new CachedConnectionToken
            {
                AccessToken = "cached-runtime-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(20),
                KeyId = "key-runtime",
                Scope = "artifact:push"
            }, JsonOptions()));

        using var client = new HttpClient(new CountingHandler(_ => throw new InvalidOperationException("HTTP should not be called.")));
        var provider = new RuntimeAccessTokenProvider(
            client,
            new StaticControlPlaneSecretProtector("runtime-secret"),
            new DefaultConnectionScopeFormatter(),
            new ConnectionTokenCacheKeyBuilder(),
            cache);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://runtime.example.test/ping");
        await provider.AttachToken(request, node, scopes);

        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("cached-runtime-token", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task RuntimeAccessTokenProviderRequestsAndCachesNewToken()
    {
        var node = CreateRuntimeNode();
        var handler = new CountingHandler(async request =>
        {
            Assert.Equal("https://runtime.example.test/runtime/distribution/connect/token", request.RequestUri!.ToString());
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"clientId\":\"runtime-client\"", body, StringComparison.Ordinal);
            Assert.Contains("\"clientSecret\":\"runtime-secret\"", body, StringComparison.Ordinal);
            Assert.Contains("\"scope\":\"connection:validate\"", body, StringComparison.Ordinal);

            return JsonResponse(new ConnectionTokenResponse
            {
                AccessToken = "fresh-runtime-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                ExpiresIn = 600,
                KeyId = "key-runtime",
                Scope = "connection:validate"
            });
        });

        var cache = new DictionaryDistributedCache();
        var provider = new RuntimeAccessTokenProvider(
            new HttpClient(handler),
            new StaticControlPlaneSecretProtector("runtime-secret"),
            new DefaultConnectionScopeFormatter(),
            new ConnectionTokenCacheKeyBuilder(),
            cache);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://runtime.example.test/ping");
        await provider.AttachToken(request, node, [ArtifactDeliveryScope.ConnectionValidate]);

        Assert.Equal("fresh-runtime-token", request.Headers.Authorization?.Parameter);
        Assert.Equal(1, handler.CallCount);

        using var second = new HttpRequestMessage(HttpMethod.Get, "https://runtime.example.test/ping");
        await provider.AttachToken(second, node, [ArtifactDeliveryScope.ConnectionValidate]);

        Assert.Equal("fresh-runtime-token", second.Headers.Authorization?.Parameter);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task RuntimeAccessTokenProviderReportsMissingAndExpiredTokens()
    {
        var missing = CreateRuntimeNode();
        missing.ProtectedOutboundSecret = string.Empty;
        var provider = new RuntimeAccessTokenProvider(
            new HttpClient(new CountingHandler(_ => Task.FromResult(JsonResponse(new ConnectionTokenResponse())))),
            new StaticControlPlaneSecretProtector("runtime-secret"),
            new DefaultConnectionScopeFormatter(),
            new ConnectionTokenCacheKeyBuilder(),
            new DictionaryDistributedCache());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.AttachToken(new HttpRequestMessage(HttpMethod.Get, "https://runtime.example.test"), missing, [ArtifactDeliveryScope.ArtifactPush]));

        var expiredProvider = new RuntimeAccessTokenProvider(
            new HttpClient(new CountingHandler(_ => Task.FromResult(JsonResponse(new ConnectionTokenResponse
            {
                AccessToken = "expired",
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1),
                KeyId = "key-runtime",
                Scope = "artifact:push"
            })))),
            new StaticControlPlaneSecretProtector("runtime-secret"),
            new DefaultConnectionScopeFormatter(),
            new ConnectionTokenCacheKeyBuilder(),
            new DictionaryDistributedCache());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            expiredProvider.AttachToken(new HttpRequestMessage(HttpMethod.Get, "https://runtime.example.test"), CreateRuntimeNode(), [ArtifactDeliveryScope.ArtifactPush]));
    }

    [Fact]
    public async Task ControlPlaneAccessTokenProviderUsesCacheAndFetchesWhenNeeded()
    {
        var source = CreateControlPlaneSource();
        var cache = new DictionaryDistributedCache();
        var formatter = new DefaultConnectionScopeFormatter();
        var keyBuilder = new ConnectionTokenCacheKeyBuilder();
        await cache.SetStringAsync(
            keyBuilder.BuildConsumerTokenKey("runtime", source.Key, formatter.FormatMany([ArtifactDeliveryScope.ReleaseRead])),
            JsonSerializer.Serialize(new CachedConnectionToken
            {
                AccessToken = "cached-control-plane-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(20),
                KeyId = "key-cp",
                Scope = "release:read"
            }, JsonOptions()));

        var handler = new CountingHandler(async request =>
        {
            Assert.Equal("https://control-plane.example.test/distribution/connect/token", request.RequestUri!.ToString());
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"clientId\":\"control-plane-client\"", body, StringComparison.Ordinal);
            Assert.Contains("\"clientSecret\":\"control-plane-secret\"", body, StringComparison.Ordinal);

            return JsonResponse(new ConnectionTokenResponse
            {
                AccessToken = "fresh-control-plane-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                ExpiresIn = 600,
                KeyId = "key-cp",
                Scope = "artifact:read"
            });
        });

        var provider = new ControlPlaneAccessTokenProvider(
            new HttpClient(handler),
            new StaticRuntimeSecretProtector("control-plane-secret"),
            formatter,
            keyBuilder,
            cache);

        using var cachedRequest = new HttpRequestMessage(HttpMethod.Get, "https://control-plane.example.test/pending");
        await provider.AttachToken(cachedRequest, source, [ArtifactDeliveryScope.ReleaseRead]);
        Assert.Equal("cached-control-plane-token", cachedRequest.Headers.Authorization?.Parameter);
        Assert.Equal(0, handler.CallCount);

        using var freshRequest = new HttpRequestMessage(HttpMethod.Get, "https://control-plane.example.test/artifacts");
        await provider.AttachToken(freshRequest, source, [ArtifactDeliveryScope.ArtifactRead]);
        Assert.Equal("fresh-control-plane-token", freshRequest.Headers.Authorization?.Parameter);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ControlPlaneAccessTokenProviderReportsMissingAndExpiredTokens()
    {
        var source = CreateControlPlaneSource();
        source.ClientId = string.Empty;
        var provider = new ControlPlaneAccessTokenProvider(
            new HttpClient(new CountingHandler(_ => Task.FromResult(JsonResponse(new ConnectionTokenResponse())))),
            new StaticRuntimeSecretProtector("control-plane-secret"),
            new DefaultConnectionScopeFormatter(),
            new ConnectionTokenCacheKeyBuilder(),
            new DictionaryDistributedCache());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.AttachToken(new HttpRequestMessage(HttpMethod.Get, "https://control-plane.example.test"), source, [ArtifactDeliveryScope.ArtifactRead]));

        var expiredProvider = new ControlPlaneAccessTokenProvider(
            new HttpClient(new CountingHandler(_ => Task.FromResult(JsonResponse(new ConnectionTokenResponse
            {
                AccessToken = "expired",
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1),
                KeyId = "key-cp",
                Scope = "artifact:read"
            })))),
            new StaticRuntimeSecretProtector("control-plane-secret"),
            new DefaultConnectionScopeFormatter(),
            new ConnectionTokenCacheKeyBuilder(),
            new DictionaryDistributedCache());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            expiredProvider.AttachToken(new HttpRequestMessage(HttpMethod.Get, "https://control-plane.example.test"), CreateControlPlaneSource(), [ArtifactDeliveryScope.ArtifactRead]));
    }

    private static RuntimeNode CreateRuntimeNode()
        => new()
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Runtime",
            Code = "runtime",
            DistributionMode = DistributionMode.Push,
            EndpointBaseUri = "https://runtime.example.test/",
            OutboundClientId = "runtime-client",
            ProtectedOutboundSecret = "protected-runtime-secret",
            TokenRefreshSkewSeconds = 60
        };

    private static ControlPlaneDistributionSource CreateControlPlaneSource()
        => new()
        {
            Key = "control-plane",
            Name = "Control Plane",
            EndpointBaseUri = "https://control-plane.example.test/",
            ClientId = "control-plane-client",
            ProtectedSecret = "protected-control-plane-secret",
            TokenRefreshSkewSeconds = 60,
            IsEnabled = true
        };

    private static HttpResponseMessage JsonResponse(ConnectionTokenResponse response)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response, JsonOptions()), Encoding.UTF8, "application/json")
        };

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);

    private sealed class StaticControlPlaneSecretProtector(string secret) : IControlPlaneRuntimeNodeSecretProtector
    {
        public string Protect(string secret) => $"protected:{secret}";
        public string Unprotect(string protectedSecret) => secret;
    }

    private sealed class StaticRuntimeSecretProtector(string secret) : IRuntimeDesignNodeSecretProtector
    {
        public string Protect(string secret) => $"protected:{secret}";
        public string Unprotect(string protectedSecret) => secret;
    }

    private sealed class CountingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return await handler(request);
        }
    }

    private sealed class DictionaryDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> values = new(StringComparer.Ordinal);

        public byte[]? Get(string key) => values.GetValueOrDefault(key);

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => values[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => values.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
