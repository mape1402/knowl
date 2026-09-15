namespace KnOwl.ControlPlane.WebUI.ButterMorph;

/// <summary>
/// Builds and parses context keys used to connect KnOwl pages with ButterMorph designer sessions.
/// </summary>
public static class KnOwlButterMorphContext
{
    /// <summary>
    /// Creates a context key for a new schema type definition.
    /// </summary>
    public static string NewType() => "type:new";

    /// <summary>
    /// Creates a context key for adding a new version to an existing schema type.
    /// </summary>
    public static string NewTypeVersion(Guid typeId) => $"type-version:new:{typeId}";

    /// <summary>
    /// Creates a context key for editing an existing metadata field.
    /// </summary>
    public static string EditMetadataField(Guid id) => $"metadata-field:{id}";

    /// <summary>
    /// Creates a context key for adding a new version to an existing metadata field.
    /// </summary>
    public static string NewMetadataFieldVersion(Guid id) => $"metadata-field-version:new:{id}";

    /// <summary>
    /// Creates a context key for a new metadata field.
    /// </summary>
    public static string NewMetadataField() => "metadata-field:new";

    /// <summary>
    /// Creates a context key for a new event definition.
    /// </summary>
    public static string EventNew() => $"event:new:{Guid.NewGuid():N}";

    /// <summary>
    /// Creates a context key for adding a new version to an existing event.
    /// </summary>
    public static string EventVersion(Guid eventId) => $"event-version:new:{eventId}";

    /// <summary>
    /// Creates a context key for a new command definition.
    /// </summary>
    public static string CommandNew() => $"command:new:{Guid.NewGuid():N}";

    /// <summary>
    /// Creates a draft context key for the request schema of a new command definition.
    /// </summary>
    public static string CommandCreateRequestDraft(Guid draftId) => $"command-create-request:new:{draftId}";

    /// <summary>
    /// Creates a draft context key for the reply schema of a new command definition.
    /// </summary>
    public static string CommandCreateReplyDraft(Guid draftId) => $"command-create-reply:new:{draftId}";

    /// <summary>
    /// Creates a context key for adding a new version to an existing command.
    /// </summary>
    public static string CommandVersion(Guid commandId) => $"command-version:new:{commandId}";

    /// <summary>
    /// Creates a draft context key for adding a command request schema.
    /// </summary>
    public static string CommandVersionRequestDraft(Guid commandId) => $"command-version-request:new:{commandId}";

    /// <summary>
    /// Creates a draft context key for adding a command reply schema.
    /// </summary>
    public static string CommandVersionReplyDraft(Guid commandId) => $"command-version-reply:new:{commandId}";

    /// <summary>
    /// Attempts to read a GUID identifier from a context key with the expected prefix.
    /// </summary>
    public static bool TryReadGuid(string contextKey, string prefix, out Guid id)
    {
        id = Guid.Empty;
        if (!contextKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Guid.TryParse(contextKey[prefix.Length..], out id);
    }
}

