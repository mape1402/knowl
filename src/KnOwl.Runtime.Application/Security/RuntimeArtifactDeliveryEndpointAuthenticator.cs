using KnOwl.Contracts.Security;
using Microsoft.AspNetCore.Http;

namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Default Runtime authenticator for Control Plane artifact push requests.
/// </summary>
public sealed class RuntimeArtifactDeliveryEndpointAuthenticator(IRuntimeConnectionTokenValidator tokenValidator) : IRuntimeArtifactDeliveryEndpointAuthenticator
{
    /// <inheritdoc />
    public async Task<RuntimeArtifactDeliveryEndpointAuthenticationResult> Authenticate(
        HttpRequest request,
        string body,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await tokenValidator.Validate(
            ReadBearerToken(request),
            [ArtifactDeliveryScope.ArtifactPush],
            cancellationToken);

        return validation.Succeeded
            ? RuntimeArtifactDeliveryEndpointAuthenticationResult.Success(validation.Principal?.NodeKey ?? string.Empty)
            : RuntimeArtifactDeliveryEndpointAuthenticationResult.Failure(validation.Message);
    }

    private static string ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : string.Empty;
    }
}
