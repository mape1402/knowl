using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KnOwl.Contracts.Security;
using KnOwl.Runtime.Distribution;
using Microsoft.Extensions.Caching.Distributed;

namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Default cached token provider for Runtime calls into Control Plane.
/// </summary>
public sealed class ControlPlaneAccessTokenProvider(
    HttpClient httpClient,
    IRuntimeDesignNodeSecretProtector secretProtector,
    IConnectionScopeFormatter scopeFormatter,
    ConnectionTokenCacheKeyBuilder cacheKeyBuilder,
    IDistributedCache cache) : IControlPlaneAccessTokenProvider
{
    /// <inheritdoc />
    public async Task AttachToken(
        HttpRequestMessage request,
        ControlPlaneDistributionSource source,
        IReadOnlyCollection<ArtifactDeliveryScope> scopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(source);

        var scopeValue = scopeFormatter.FormatMany(scopes);
        var token = await GetToken(source, scopeValue, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<string> GetToken(ControlPlaneDistributionSource source, string scopeValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(source.ProtectedSecret) || string.IsNullOrWhiteSpace(source.ClientId))
        {
            throw new InvalidOperationException($"Control Plane source '{source.Name}' does not have outbound credentials.");
        }

        var cacheKey = cacheKeyBuilder.BuildConsumerTokenKey("runtime", source.Key, scopeValue);
        var cachedJson = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedJson))
        {
            var cached = JsonSerializer.Deserialize<CachedConnectionToken>(cachedJson, JsonOptions());
            if (cached is not null && cached.ExpiresAtUtc > DateTime.UtcNow.AddSeconds(source.TokenRefreshSkewSeconds))
            {
                return cached.AccessToken;
            }
        }

        var secret = secretProtector.Unprotect(source.ProtectedSecret);
        var body = JsonSerializer.Serialize(new ConnectionTokenRequest
        {
            GrantType = "client_credentials",
            ClientId = source.ClientId,
            ClientSecret = secret,
            Scope = scopeValue
        }, JsonOptions());

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, BuildTokenUri(source))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        using var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = JsonSerializer.Deserialize<ConnectionTokenResponse>(responseBody, JsonOptions())
            ?? throw new InvalidOperationException("Control Plane returned an empty token response.");

        var ttl = token.ExpiresAtUtc - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Control Plane returned an expired token.");
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

    private static Uri BuildTokenUri(ControlPlaneDistributionSource source)
        => new($"{source.EndpointBaseUri.TrimEnd('/')}/distribution/connect/token", UriKind.Absolute);

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}
