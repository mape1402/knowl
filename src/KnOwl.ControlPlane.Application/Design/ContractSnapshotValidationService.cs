using System.Text.Json;
using KnOwl.ControlPlane.Design.Storage;

namespace KnOwl.ControlPlane.Application;

/// <inheritdoc />
internal sealed class ContractSnapshotValidationService(
    IEventRepository events,
    ICommandRepository commands,
    ISchemaTypeRepository schemaTypes) : IContractSnapshotValidationService
{
    /// <inheritdoc />
    public async Task<ContractSnapshotValidationResult> ValidateEventVersion(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await events.GetVersionById(versionId, cancellationToken);
        return version is null
            ? ContractSnapshotValidationResult.Invalid([$"Event version '{versionId}' was not found."])
            : await ValidatePayloadSchema(version.PayloadSchemaJson, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractSnapshotValidationResult> ValidateCommandVersion(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await commands.GetVersionById(versionId, cancellationToken);
        return version is null
            ? ContractSnapshotValidationResult.Invalid([$"Command version '{versionId}' was not found."])
            : await ValidateCommandPayloadSchemas(version.PayloadSchemaJson, version.ReplyPayloadSchemaJson, cancellationToken);
    }

    private async Task<ContractSnapshotValidationResult> ValidateCommandPayloadSchemas(
        string requestSchemaJson,
        string? replySchemaJson,
        CancellationToken cancellationToken)
    {
        var requestValidation = await ValidatePayloadSchema(requestSchemaJson, cancellationToken);
        if (!requestValidation.IsValid)
        {
            return requestValidation;
        }

        if (string.IsNullOrWhiteSpace(replySchemaJson))
        {
            return ContractSnapshotValidationResult.Valid();
        }

        var replyValidation = await ValidatePayloadSchema(replySchemaJson, cancellationToken);
        return replyValidation.IsValid
            ? ContractSnapshotValidationResult.Valid()
            : ContractSnapshotValidationResult.Invalid(replyValidation.Errors.Select(x => $"Reply {x}").ToArray());
    }

    /// <inheritdoc />
    public async Task<ContractSnapshotValidationResult> ValidatePayloadSchema(string payloadSchemaJson, CancellationToken cancellationToken = default)
    {
        List<string> errors = [];
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payloadSchemaJson) ? "{}" : payloadSchemaJson);
        }
        catch (JsonException ex)
        {
            return ContractSnapshotValidationResult.Invalid([$"Payload schema is not valid JSON: {ex.Message}"]);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return ContractSnapshotValidationResult.Invalid(["Payload schema root must be a JSON object."]);
            }

            var defs = GetDefs(root);
            HashSet<Guid> typeVersionIds = [];
            ValidateElement(root, "$", defs, typeVersionIds, errors);

            foreach (var typeVersionId in typeVersionIds)
            {
                var typeVersion = await schemaTypes.GetVersionById(typeVersionId, cancellationToken);
                if (typeVersion is null)
                {
                    errors.Add($"Referenced schema type version '{typeVersionId}' does not exist.");
                }
            }
        }

        return errors.Count == 0
            ? ContractSnapshotValidationResult.Valid()
            : ContractSnapshotValidationResult.Invalid(errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static HashSet<string> GetDefs(JsonElement root)
    {
        if (!root.TryGetProperty("$defs", out var defs) || defs.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return defs.EnumerateObject()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static void ValidateElement(
        JsonElement element,
        string path,
        HashSet<string> defs,
        HashSet<Guid> typeVersionIds,
        List<string> errors)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                ValidateObject(element, path, defs, typeVersionIds, errors);
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    ValidateElement(item, $"{path}[{index++}]", defs, typeVersionIds, errors);
                }

                break;
        }
    }

    private static void ValidateObject(
        JsonElement element,
        string path,
        HashSet<string> defs,
        HashSet<Guid> typeVersionIds,
        List<string> errors)
    {
        if (element.TryGetProperty("$ref", out var refElement) && refElement.ValueKind == JsonValueKind.String)
        {
            ValidateRef(refElement.GetString(), path, defs, errors);
        }

        if (element.TryGetProperty("typeVersionId", out var typeVersionElement) &&
            typeVersionElement.ValueKind == JsonValueKind.String &&
            Guid.TryParse(typeVersionElement.GetString(), out var typeVersionId))
        {
            typeVersionIds.Add(typeVersionId);
        }

        foreach (var property in element.EnumerateObject())
        {
            ValidateElement(property.Value, $"{path}.{property.Name}", defs, typeVersionIds, errors);
        }
    }

    private static void ValidateRef(string? refValue, string path, HashSet<string> defs, List<string> errors)
    {
        const string prefix = "#/$defs/";
        if (string.IsNullOrWhiteSpace(refValue) || !refValue.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        var defName = refValue[prefix.Length..];
        if (!defs.Contains(defName))
        {
            errors.Add($"Reference '{refValue}' at '{path}' does not have a matching $defs entry.");
        }
    }
}
