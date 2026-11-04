using Microsoft.Extensions.Options;

namespace KnOwl.ControlPlane.WebUI.ButterMorph;

/// <summary>
/// Stores short-lived ButterMorph results before the surrounding KnOwl form is persisted.
/// </summary>
public sealed class KnOwlButterMorphDraftStore
{
    private static readonly TimeSpan DefaultRetention = TimeSpan.FromMinutes(20);
    private const int DefaultMaximumEntries = 256;
    private readonly object sync = new();
    private readonly Dictionary<string, DraftEntry> entries = new(StringComparer.Ordinal);
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan retention;
    private readonly int maximumEntries;

    /// <summary>
    /// Initializes the store with its default bounded retention policy.
    /// </summary>
    public KnOwlButterMorphDraftStore()
        : this(TimeProvider.System, new KnOwlButterMorphDraftStoreOptions())
    {
    }

    /// <summary>
    /// Initializes the store with the policy registered by the host.
    /// </summary>
    public KnOwlButterMorphDraftStore(IOptions<KnOwlButterMorphDraftStoreOptions> options)
        : this(TimeProvider.System, options?.Value ?? new KnOwlButterMorphDraftStoreOptions())
    {
    }

    /// <summary>
    /// Initializes the store with a testable clock and retention policy.
    /// </summary>
    public KnOwlButterMorphDraftStore(TimeProvider timeProvider, KnOwlButterMorphDraftStoreOptions options)
    {
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        ArgumentNullException.ThrowIfNull(options);
        retention = options.Retention > TimeSpan.Zero ? options.Retention : DefaultRetention;
        maximumEntries = options.MaximumEntries > 0 ? options.MaximumEntries : DefaultMaximumEntries;
    }

    /// <summary>
    /// Saves a draft payload schema for a designer context.
    /// </summary>
    public void SavePayloadSchema(string contextKey, string payloadSchemaJson)
    {
        if (string.IsNullOrWhiteSpace(contextKey))
        {
            return;
        }

        lock (sync)
        {
            GetOrCreate(contextKey).PayloadSchemaJson = payloadSchemaJson;
        }
    }

    /// <summary>
    /// Gets a previously captured draft payload schema without consuming it.
    /// </summary>
    public string GetPayloadSchema(string contextKey)
    {
        lock (sync)
        {
            return TryGet(contextKey, out var entry) ? entry.PayloadSchemaJson : string.Empty;
        }
    }

    /// <summary>
    /// Gets and removes a previously captured draft payload schema for a designer context.
    /// </summary>
    public string TakePayloadSchema(string contextKey)
    {
        lock (sync)
        {
            if (!TryGet(contextKey, out var entry))
            {
                return string.Empty;
            }

            var payloadSchema = entry.PayloadSchemaJson;
            entry.PayloadSchemaJson = string.Empty;
            RemoveIfEmpty(contextKey, entry);
            return payloadSchema;
        }
    }

    /// <summary>
    /// Records the event created from a designer context.
    /// </summary>
    public void SaveCreatedEvent(string contextKey, Guid eventId)
    {
        if (string.IsNullOrWhiteSpace(contextKey))
        {
            return;
        }

        lock (sync)
        {
            GetOrCreate(contextKey).CreatedEventId = eventId;
        }
    }

    /// <summary>
    /// Gets the event created from a designer context without consuming it.
    /// </summary>
    public Guid? GetCreatedEvent(string contextKey)
    {
        lock (sync)
        {
            return TryGet(contextKey, out var entry) ? entry.CreatedEventId : null;
        }
    }

    /// <summary>
    /// Gets and removes the event created from a designer context, when available.
    /// </summary>
    public Guid? TakeCreatedEvent(string contextKey)
    {
        lock (sync)
        {
            if (!TryGet(contextKey, out var entry))
            {
                return null;
            }

            var eventId = entry.CreatedEventId;
            entry.CreatedEventId = null;
            RemoveIfEmpty(contextKey, entry);
            return eventId;
        }
    }

    /// <summary>
    /// Records the command created from a designer context.
    /// </summary>
    public void SaveCreatedCommand(string contextKey, Guid commandId)
    {
        if (string.IsNullOrWhiteSpace(contextKey))
        {
            return;
        }

        lock (sync)
        {
            GetOrCreate(contextKey).CreatedCommandId = commandId;
        }
    }

    /// <summary>
    /// Gets the command created from a designer context without consuming it.
    /// </summary>
    public Guid? GetCreatedCommand(string contextKey)
    {
        lock (sync)
        {
            return TryGet(contextKey, out var entry) ? entry.CreatedCommandId : null;
        }
    }

    /// <summary>
    /// Gets and removes the command created from a designer context, when available.
    /// </summary>
    public Guid? TakeCreatedCommand(string contextKey)
    {
        lock (sync)
        {
            if (!TryGet(contextKey, out var entry))
            {
                return null;
            }

            var commandId = entry.CreatedCommandId;
            entry.CreatedCommandId = null;
            RemoveIfEmpty(contextKey, entry);
            return commandId;
        }
    }

    /// <summary>
    /// Removes every expired transient result and returns the number of released contexts.
    /// </summary>
    public int PurgeExpired()
    {
        lock (sync)
        {
            return PurgeExpiredCore();
        }
    }

    private DraftEntry GetOrCreate(string contextKey)
    {
        PurgeExpiredCore();
        if (entries.TryGetValue(contextKey, out var existing))
        {
            existing.ExpiresAtUtc = timeProvider.GetUtcNow().Add(retention);
            return existing;
        }

        if (entries.Count >= maximumEntries)
        {
            var oldest = entries
                .OrderBy(x => x.Value.ExpiresAtUtc)
                .ThenBy(x => x.Key, StringComparer.Ordinal)
                .First();
            entries.Remove(oldest.Key);
        }

        var entry = new DraftEntry
        {
            ExpiresAtUtc = timeProvider.GetUtcNow().Add(retention)
        };
        entries[contextKey] = entry;
        return entry;
    }

    private bool TryGet(string? contextKey, out DraftEntry entry)
    {
        PurgeExpiredCore();
        if (string.IsNullOrWhiteSpace(contextKey) || !entries.TryGetValue(contextKey, out entry!))
        {
            entry = null!;
            return false;
        }

        return true;
    }

    private int PurgeExpiredCore()
    {
        var now = timeProvider.GetUtcNow();
        var expired = entries
            .Where(x => x.Value.ExpiresAtUtc <= now)
            .Select(x => x.Key)
            .ToArray();
        foreach (var key in expired)
        {
            entries.Remove(key);
        }

        return expired.Length;
    }

    private void RemoveIfEmpty(string contextKey, DraftEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.PayloadSchemaJson) &&
            !entry.CreatedEventId.HasValue &&
            !entry.CreatedCommandId.HasValue)
        {
            entries.Remove(contextKey);
        }
    }

    private sealed class DraftEntry
    {
        internal string PayloadSchemaJson { get; set; } = string.Empty;
        internal Guid? CreatedEventId { get; set; }
        internal Guid? CreatedCommandId { get; set; }
        internal DateTimeOffset ExpiresAtUtc { get; set; }
    }
}
