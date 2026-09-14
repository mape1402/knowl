using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Contracts.Security;
using Microsoft.Extensions.Caching.Distributed;

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Default cached token provider for Control Plane calls into Runtime.
/// </summary>
public sealed class RuntimeAccessTokenProvider(
    HttpClient httpClient,
    IControlPlaneRuntimeNodeSecretProtector secretProtector,
    IConnectionScopeFormatter scopeFormatter,
    ConnectionTokenCacheKeyBuilder cacheKeyBuilder,
    IDistributedCache cache) : IRuntimeAccessTokenProvider
{
    /// <inheritdoc />
    public async Task AttachToken(
        HttpRequestMessage request,
        RuntimeNode node,
        IReadOnlyCollection<ArtifactDeliveryScope> scopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(node);

        var scopeValue = scopeFormatter.FormatMany(scopes);
        var token = await GetToken(node, scopeValue, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<string> GetToken(RuntimeNode node, string scopeValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(node.ProtectedOutboundSecret) || string.IsNullOrWhiteSpace(node.OutboundClientId))
        {
            throw new InvalidOperationException($"Runtime node '{node.Name}' does not have outbound credentials.");
        }

        var cacheKey = cacheKeyBuilder.BuildConsumerTokenKey("control-plane", node.Id.ToString("N"), scopeValue);
        var cachedJson = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedJson))
        {
            var cached = JsonSerializer.Deserialize<CachedConnectionToken>(cachedJson, JsonOptions());
            if (cached is not null && cached.ExpiresAtUtc > DateTime.UtcNow.AddSeconds(node.TokenRefreshSkewSeconds))
            {
                return cached.AccessToken;
            }
        }

        var secret = secretProtector.Unprotect(node.ProtectedOutboundSecret);
        var body = JsonSerializer.Serialize(new ConnectionTokenRequest
        {
            GrantType = "client_credentials",
            ClientId = node.OutboundClientId,
            ClientSecret = secret,
            Scope = scopeValue
        }, JsonOptions());

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, BuildTokenUri(node))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        using var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = JsonSerializer.Deserialize<ConnectionTokenResponse>(responseBody, JsonOptions())
            ?? throw new InvalidOperationException("Runtime returned an empty token response.");

        var ttl = token.ExpiresAtUtc - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Runtime returned an expired token.");
        }

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(new CachedConnectionToken
            {
                AccessToken = token.AccessToken,
                ExpiresAtUtc = token.ExpiresAtUtc,
                KeyId = token.KeyId,
                Scope = token.Scope
            }, JsonOptions()),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);

        return token.AccessToken;
    }

    private static Uri BuildTokenUri(RuntimeNode node)
        => new($"{node.EndpointBaseUri.TrimEnd('/')}/runtime/distribution/connect/token", UriKind.Absolute);

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}
