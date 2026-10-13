namespace KnOwl.ControlPlane.WebUI.ButterMorph;

using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using global::ButterMorph.SchemaDesign;
using global::ButterMorph.Web.Razor;

/// <summary>
/// Supplies read-only contract payload schemas to ButterMorph's schema viewer.
/// </summary>
public sealed class KnOwlPayloadSchemaViewerHost(
    IEventInteractionService events,
    ICommandInteractionService commands,
    IContractArtifactRepository artifacts) : IButterMorphPayloadSchemaViewerHost
{
    /// <inheritdoc />
    public async Task<ButterMorphPayloadSchemaViewerLoadResult> Load(ButterMorphPayloadSchemaViewerLoadRequest request)
    {
        var contextKey = request.ContextKey ?? string.Empty;
        if (KnOwlButterMorphContext.TryReadGuid(contextKey, "event-version:view:", out var eventVersionId))
        {
            return CreateResult((await events.GetVersionById(eventVersionId))?.PayloadSchemaJson, "Event schema was not found.");
        }

        if (KnOwlButterMorphContext.TryReadGuid(contextKey, "command-version-request:view:", out var commandRequestVersionId))
        {
            return CreateResult((await commands.GetVersionById(commandRequestVersionId))?.PayloadSchemaJson, "Command request schema was not found.");
        }

        if (KnOwlButterMorphContext.TryReadGuid(contextKey, "command-version-reply:view:", out var commandReplyVersionId))
        {
            return CreateResult((await commands.GetVersionById(commandReplyVersionId))?.ReplyPayloadSchemaJson, "Command reply schema was not found.");
        }

        if (TryReadArtifactContext(contextKey, out var artifactPart, out var artifactId))
        {
            return await LoadArtifactSchema(artifactId, artifactPart);
        }

        return new ButterMorphPayloadSchemaViewerLoadResult
        {
            JsonSchema = "{}",
            Message = "Schema context was not recognized."
        };
    }

    private async Task<ButterMorphPayloadSchemaViewerLoadResult> LoadArtifactSchema(Guid artifactId, string artifactPart)
    {
        var artifact = await artifacts.GetById(artifactId);
        if (artifact is null)
        {
            return CreateResult(null, "Artifact schema was not found.");
        }

        if (artifact.ArtifactType == ContractArtifactType.Command)
        {
            var payload = CommandArtifactPayloadDocument.Read(artifact.PayloadSchemaJson);
            if (string.Equals(artifactPart, "reply", StringComparison.OrdinalIgnoreCase))
            {
                return CreateResult(payload.ReplyPayloadSchemaJson, "Command reply schema was not found.");
            }

            return CreateResult(payload.RequestPayloadSchemaJson, "Command request schema was not found.");
        }

        return CreateResult(artifact.PayloadSchemaJson, "Artifact schema was not found.");
    }

    private static ButterMorphPayloadSchemaViewerLoadResult CreateResult(string? payloadSchemaJson, string missingMessage)
    {
        if (string.IsNullOrWhiteSpace(payloadSchemaJson))
        {
            return new ButterMorphPayloadSchemaViewerLoadResult
            {
                JsonSchema = "{}",
                Message = missingMessage
            };
        }

        return new ButterMorphPayloadSchemaViewerLoadResult
        {
            Definition = ReadDefinition(payloadSchemaJson),
            JsonSchema = payloadSchemaJson,
            Message = string.Empty
        };
    }

    private static PayloadSchemaDefinition? ReadDefinition(string payloadSchemaJson)
        => KnOwlButterMorphDefinitionMapper.TryReadPayloadDefinition(payloadSchemaJson, out var definition)
            ? definition
            : null;

    private static bool TryReadArtifactContext(string contextKey, out string part, out Guid artifactId)
    {
        part = string.Empty;
        artifactId = Guid.Empty;
        const string prefix = "artifact:view:";
        if (!contextKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = contextKey[prefix.Length..].Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !Guid.TryParse(parts[1], out artifactId))
        {
            return false;
        }

        part = parts[0];
        return true;
    }
}
