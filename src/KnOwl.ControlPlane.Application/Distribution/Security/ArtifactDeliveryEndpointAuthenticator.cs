using KnOwl.Contracts.Security;
using Microsoft.AspNetCore.Http;

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Default Control Plane authenticator for Runtime artifact pull requests.
/// </summary>
public sealed class ArtifactDeliveryEndpointAuthenticator(IControlPlaneConnectionTokenValidator tokenValidator) : IArtifactDeliveryEndpointAuthenticator
{
    /// <inheritdoc />
    public async Task<ArtifactDeliveryEndpointAuthenticationResult> AuthenticateRuntimeNode(
        HttpRequest request,
        Guid runtimeNodeId,
        string body,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await tokenValidator.Validate(
            ReadBearerToken(request),
            runtimeNodeId,
            ResolveRequiredScopes(request),
            cancellationToken);

        return validation.Succeeded
            ? ArtifactDeliveryEndpointAuthenticationResult.Success()
            : ArtifactDeliveryEndpointAuthenticationResult.Failure(validation.Message);
    }

    private static IReadOnlyCollection<ArtifactDeliveryScope> ResolveRequiredScopes(HttpRequest request)
    {
        if (HttpMethods.IsPost(request.Method))
        {
            return [ArtifactDeliveryScope.ArtifactAcknowledge];
        }

        return request.Path.Value?.Contains("/pending", StringComparison.OrdinalIgnoreCase) == true
            ? [ArtifactDeliveryScope.ReleaseRead]
            : [ArtifactDeliveryScope.ArtifactRead];
    }

    private static string ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : string.Empty;
    }
}
