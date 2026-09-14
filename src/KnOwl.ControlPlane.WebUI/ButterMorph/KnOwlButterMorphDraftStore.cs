namespace KnOwl.ControlPlane.WebUI.ButterMorph;

/// <summary>
/// Stores transient ButterMorph designer results before the surrounding KnOwl form is persisted.
/// </summary>
public sealed class KnOwlButterMorphDraftStore
{
    private readonly Dictionary<string, string> _payloadSchemas = [];
    private readonly Dictionary<string, Guid> _createdEvents = [];
    private readonly Dictionary<string, Guid> _createdCommands = [];

    /// <summary>
    /// Saves a draft payload schema for a designer context.
    /// </summary>
    public void SavePayloadSchema(string contextKey, string payloadSchemaJson)
    {
        if (!string.IsNullOrWhiteSpace(contextKey))
        {
            _payloadSchemas[contextKey] = payloadSchemaJson;
        }
    }

    /// <summary>
    /// Gets a previously captured draft payload schema for a designer context.
    /// </summary>
    public string GetPayloadSchema(string contextKey)
    {
        return !string.IsNullOrWhiteSpace(contextKey) && _payloadSchemas.TryGetValue(contextKey, out var value)
            ? value
            : string.Empty;
    }

    /// <summary>
    /// Records the event created from a designer context.
    /// </summary>
    public void SaveCreatedEvent(string contextKey, Guid eventId)
    {
        if (!string.IsNullOrWhiteSpace(contextKey))
        {
            _createdEvents[contextKey] = eventId;
        }
    }

    /// <summary>
    /// Gets the event created from a designer context, when available.
    /// </summary>
    public Guid? GetCreatedEvent(string contextKey)
    {
        return !string.IsNullOrWhiteSpace(contextKey) && _createdEvents.TryGetValue(contextKey, out var value)
            ? value
            : null;
    }

    /// <summary>
    /// Records the command created from a designer context.
    /// </summary>
    public void SaveCreatedCommand(string contextKey, Guid commandId)
    {
        if (!string.IsNullOrWhiteSpace(contextKey))
        {
            _createdCommands[contextKey] = commandId;
        }
    }

    /// <summary>
    /// Gets the command created from a designer context, when available.
    /// </summary>
    public Guid? GetCreatedCommand(string contextKey)
    {
        return !string.IsNullOrWhiteSpace(contextKey) && _createdCommands.TryGetValue(contextKey, out var value)
            ? value
            : null;
    }
}

