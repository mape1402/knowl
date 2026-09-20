using System.Text;
using System.Text.Json;

namespace KnOwl.Contracts.Artifacts;

/// <summary>
/// Creates and reads the payload stored by a command artifact.
/// </summary>
public static class CommandArtifactPayloadDocument
{
    /// <summary>
    /// Gets the payload document schema marker used for command artifacts.
    /// </summary>
    public const string Schema = "knowl.command-artifact.v1";

    /// <summary>
    /// Creates a command artifact payload from request and optional reply schemas.
    /// </summary>
    public static string Compose(string requestPayloadSchemaJson, string? replyPayloadSchemaJson)
    {
        using var request = ParseJson(requestPayloadSchemaJson, "request");
        using var reply = string.IsNullOrWhiteSpace(replyPayloadSchemaJson)
            ? null
            : ParseJson(replyPayloadSchemaJson, "reply");
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("$schema", Schema);
            writer.WritePropertyName("request");
            request.RootElement.WriteTo(writer);

            if (reply is not null)
            {
                writer.WritePropertyName("reply");
                reply.RootElement.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Reads a command artifact payload into request and optional reply schemas.
    /// </summary>
    public static CommandArtifactPayloadParts Read(string payloadSchemaJson)
    {
        if (string.IsNullOrWhiteSpace(payloadSchemaJson))
        {
            return new CommandArtifactPayloadParts("{}", null);
        }

        using var payload = JsonDocument.Parse(payloadSchemaJson);
        var root = payload.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("request", out var request))
        {
            return new CommandArtifactPayloadParts(root.GetRawText(), null);
        }

        string? replySchema = null;
        if (root.TryGetProperty("reply", out var reply) && reply.ValueKind != JsonValueKind.Null)
        {
            replySchema = reply.GetRawText();
        }

        return new CommandArtifactPayloadParts(request.GetRawText(), replySchema);
    }

    private static JsonDocument ParseJson(string value, string partName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Command {partName} schema is required.", nameof(value));
        }

        try
        {
            return JsonDocument.Parse(value);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Command {partName} schema must be valid JSON.", nameof(value), ex);
        }
    }
}

/// <summary>
/// Represents the schemas contained by one command artifact payload.
/// </summary>
public sealed record CommandArtifactPayloadParts(string RequestPayloadSchemaJson, string? ReplyPayloadSchemaJson);
