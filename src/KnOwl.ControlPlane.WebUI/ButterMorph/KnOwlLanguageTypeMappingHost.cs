namespace KnOwl.ControlPlane.WebUI.ButterMorph;

using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using global::ButterMorph.Language;
using global::ButterMorph.Web.Razor;

/// <summary>
/// Supplies host-owned type mapping documents and bundles to ButterMorph language viewers.
/// </summary>
public sealed class KnOwlLanguageTypeMappingHost(
    ISchemaTypeInteractionService schemaTypes,
    ILanguageTypeMappingInteractionService mappings,
    ILanguageTypeBundleInteractionService bundles) : IButterMorphLanguageTypeMappingHost
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    /// <inheritdoc />
    public async Task<ButterMorphLanguageTypeMappingLoadResult> Load(ButterMorphLanguageTypeMappingLoadRequest request)
    {
        var languageKey = NormalizeLanguageKey(request.LanguageKey);
        var mappingRows = await mappings.GetActive();
        var bundleRows = await bundles.GetActive();
        var documents = mappingRows
            .Where(x => string.IsNullOrWhiteSpace(languageKey) || string.Equals(x.LanguageKey, languageKey, StringComparison.OrdinalIgnoreCase))
            .Select(ReadMappingDocument)
            .Where(x => x is not null)
            .Cast<ButterMorphLanguageTypeMappingDocument>()
            .ToArray();
        var resolvedBundles = bundleRows
            .Where(x => string.IsNullOrWhiteSpace(languageKey) || string.Equals(x.LanguageKey, languageKey, StringComparison.OrdinalIgnoreCase))
            .Select(ReadBundle)
            .Where(x => x is not null)
            .Cast<ButterMorphLanguageTypeBundle>()
            .ToArray();
        var activeDocumentKey = ResolveActiveDocumentKey(documents, request.ContextKey);

        return new ButterMorphLanguageTypeMappingLoadResult
        {
            ButterMorphTypes = await CreateButterMorphTypeCatalog(),
            Documents = documents,
            Profiles = documents.Select(document => ButterMorphLanguageTypeMappingDocumentConverter.ToProfile(document)).ToArray(),
            Bundles = resolvedBundles,
            ActiveDocumentKey = activeDocumentKey,
            ActiveProfileId = activeDocumentKey
        };
    }

    /// <inheritdoc />
    public async Task<ButterMorphLanguageTypeMappingSaveResult> SaveProfile(ButterMorphLanguageTypeMappingSaveRequest request)
    {
        var document = request.Document;
        if (string.IsNullOrWhiteSpace(document?.Key) && request.Profile is not null)
        {
            document = ButterMorphLanguageTypeMappingDocumentConverter.FromProfile(request.Profile);
        }

        document ??= new ButterMorphLanguageTypeMappingDocument();
        NormalizeDocument(document);
        await mappings.Upsert(ToEntity(document));

        return new ButterMorphLanguageTypeMappingSaveResult
        {
            Succeeded = true,
            Document = document,
            Profile = ButterMorphLanguageTypeMappingDocumentConverter.ToProfile(document),
            Message = "Language type mappings saved."
        };
    }

    /// <summary>
    /// Normalizes and serializes one ButterMorph mapping document into a KnOwl entity.
    /// </summary>
    public static LanguageTypeMappingDefinition ToEntity(ButterMorphLanguageTypeMappingDocument document)
    {
        NormalizeDocument(document);
        return new LanguageTypeMappingDefinition
        {
            Key = document.Key,
            Name = document.Name,
            Description = document.Description,
            LanguageKey = NormalizeLanguageKey(document.Language),
            DocumentJson = JsonSerializer.Serialize(document, JsonOptions),
            IsActive = true
        };
    }

    /// <summary>
    /// Normalizes and serializes one ButterMorph bundle into a KnOwl entity.
    /// </summary>
    public static LanguageTypeBundleDefinition ToEntity(ButterMorphLanguageTypeBundle bundle)
    {
        NormalizeBundle(bundle);
        return new LanguageTypeBundleDefinition
        {
            AssemblyId = bundle.AssemblyId,
            AssemblyKey = bundle.AssemblyKey,
            AssemblyDescription = bundle.AssemblyDescription,
            AssemblyVersion = bundle.AssemblyVersion,
            LanguageKey = NormalizeLanguageKey(bundle.LanguageKey),
            BundleJson = JsonSerializer.Serialize(bundle, JsonOptions),
            IsActive = true
        };
    }

    /// <summary>
    /// Reads a persisted mapping document.
    /// </summary>
    public static ButterMorphLanguageTypeMappingDocument? ReadMappingDocument(LanguageTypeMappingDefinition mapping)
    {
        try
        {
            var document = JsonSerializer.Deserialize<ButterMorphLanguageTypeMappingDocument>(mapping.DocumentJson, JsonOptions);
            if (document is null)
            {
                return null;
            }

            NormalizeDocument(document);
            return document;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads a persisted language type bundle.
    /// </summary>
    public static ButterMorphLanguageTypeBundle? ReadBundle(LanguageTypeBundleDefinition bundle)
    {
        try
        {
            var document = JsonSerializer.Deserialize<ButterMorphLanguageTypeBundle>(bundle.BundleJson, JsonOptions);
            if (document is null)
            {
                return null;
            }

            NormalizeBundle(document);
            return document;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<IReadOnlyCollection<ButterMorphTypeMappingCatalogItem>> CreateButterMorphTypeCatalog()
    {
        var versions = await schemaTypes.GetActiveVersions();
        return versions
            .Where(x => x.SchemaTypeDefinition is not null)
            .Select(version => new ButterMorphTypeMappingCatalogItem
            {
                Key = version.SchemaTypeDefinition!.Key,
                DisplayName = version.SchemaTypeDefinition.Name,
                Version = version.SchemaTypeDefinition.IsSystem ? "builtin" : version.VersionNumber,
                Description = version.SchemaTypeDefinition.Description ?? version.Comment ?? string.Empty
            })
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ResolveActiveDocumentKey(
        IReadOnlyCollection<ButterMorphLanguageTypeMappingDocument> documents,
        string contextKey)
    {
        if (!string.IsNullOrWhiteSpace(contextKey) &&
            documents.Any(x => string.Equals(x.Key, contextKey, StringComparison.OrdinalIgnoreCase)))
        {
            return contextKey;
        }

        return documents.FirstOrDefault()?.Key ?? string.Empty;
    }

    private static void NormalizeDocument(ButterMorphLanguageTypeMappingDocument document)
    {
        ButterMorphLanguageTypeMappingDocumentConverter.Normalize(document);
        document.Language = NormalizeLanguageKey(document.Language);
        document.Key = string.IsNullOrWhiteSpace(document.Key)
            ? KnOwlButterMorphDefinitionMapper.NormalizeKey($"{document.Language}-type-mappings")
            : KnOwlButterMorphDefinitionMapper.NormalizeKey(document.Key);
        document.Name = string.IsNullOrWhiteSpace(document.Name) ? document.Key : document.Name.Trim();
        document.Description = document.Description?.Trim() ?? string.Empty;
    }

    private static void NormalizeBundle(ButterMorphLanguageTypeBundle bundle)
    {
        bundle.LanguageKey = NormalizeLanguageKey(bundle.LanguageKey);
        bundle.AssemblyKey = string.IsNullOrWhiteSpace(bundle.AssemblyKey)
            ? KnOwlButterMorphDefinitionMapper.NormalizeKey(bundle.AssemblyId)
            : KnOwlButterMorphDefinitionMapper.NormalizeKey(bundle.AssemblyKey);
        bundle.AssemblyId = string.IsNullOrWhiteSpace(bundle.AssemblyId)
            ? bundle.AssemblyKey
            : bundle.AssemblyId.Trim();
        bundle.AssemblyVersion = string.IsNullOrWhiteSpace(bundle.AssemblyVersion)
            ? "1.0.0"
            : bundle.AssemblyVersion.Trim();
        bundle.AssemblyDescription = bundle.AssemblyDescription?.Trim() ?? string.Empty;
        bundle.Types ??= [];
    }

    private static string NormalizeLanguageKey(string? languageKey)
        => string.IsNullOrWhiteSpace(languageKey) ? "csharp" : languageKey.Trim().ToLowerInvariant();
}
