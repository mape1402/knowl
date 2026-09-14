using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.WebUI.ContractMetadata;

public class ContractFieldMetadataInput
{
    [Required]
    [MaxLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [RegularExpression(@"^[A-Za-z][A-Za-z0-9_-]*$", ErrorMessage = "Usa letras, numeros, _ o -, comenzando con una letra.")]
    [Display(Name = "Key")]
    public string Key { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    [Display(Name = "Type de dato")]
    public string DataType { get; set; } = "string";

    [Display(Name = "Events")]
    public bool AppliesToEvents { get; set; } = true;

    [Display(Name = "Commands")]
    public bool AppliesToCommands { get; set; } = true;

    [Display(Name = "Required")]
    public bool IsRequired { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Orden")]
    public int SortOrder { get; set; }

    [Display(Name = "Min length")]
    public int? MinLength { get; set; }

    [Display(Name = "Max length")]
    public int? MaxLength { get; set; }

    [Display(Name = "Regex")]
    public string? Pattern { get; set; }

    [Display(Name = "Minimum")]
    public decimal? Minimum { get; set; }

    [Display(Name = "Maximum")]
    public decimal? Maximum { get; set; }

    [Display(Name = "Fecha minima")]
    public string? DateMinimum { get; set; }

    [Display(Name = "Fecha maxima")]
    public string? DateMaximum { get; set; }

    [Display(Name = "Valores permitidos")]
    public string? AllowedValuesText { get; set; }

    public void Validate(Action<string> addError)
    {
        if (!ContractFieldMetadataCatalog.DataTypes.Contains(DataType))
        {
            addError("Invalid data type.");
        }

        if (!AppliesToEvents && !AppliesToCommands)
        {
            addError("Select at least one applicable section.");
        }

        if (MinLength.HasValue && MinLength < 0) addError("MinLength no puede ser negativo.");
        if (MaxLength.HasValue && MaxLength < 0) addError("MaxLength no puede ser negativo.");
        if (MinLength.HasValue && MaxLength.HasValue && MinLength > MaxLength) addError("MinLength no puede ser mayor que MaxLength.");
        if (Minimum.HasValue && Maximum.HasValue && Minimum > Maximum) addError("Minimum no puede ser mayor que Maximum.");
        if (!string.IsNullOrWhiteSpace(DateMinimum) && !DateOnly.TryParse(DateMinimum, out _)) addError("Fecha minima invalida.");
        if (!string.IsNullOrWhiteSpace(DateMaximum) && !DateOnly.TryParse(DateMaximum, out _)) addError("Fecha maxima invalida.");
        if (DateOnly.TryParse(DateMinimum, out var minDate) &&
            DateOnly.TryParse(DateMaximum, out var maxDate) &&
            minDate > maxDate)
        {
            addError("Fecha minima no puede ser mayor que fecha maxima.");
        }
    }

    public string BuildAppliesToJson()
    {
        var sections = new List<string>();
        if (AppliesToEvents) sections.Add("events");
        if (AppliesToCommands) sections.Add("commands");
        return JsonSerializer.Serialize(sections);
    }

    public string BuildValidationJson()
    {
        var validation = new Dictionary<string, object?>();
        var allowedValues = DataType is "boolean" or "date" ? [] : ParseAllowedValues();

        switch (DataType)
        {
            case "string":
                AddIfValue(validation, "minLength", MinLength);
                AddIfValue(validation, "maxLength", MaxLength);
                AddIfText(validation, "pattern", Pattern);
                AddAllowedValues(validation, allowedValues);
                break;
            case "number":
            case "integer":
                AddIfValue(validation, "minimum", Minimum);
                AddIfValue(validation, "maximum", Maximum);
                AddAllowedValues(validation, allowedValues);
                break;
            case "date":
                AddIfText(validation, "minimum", DateMinimum);
                AddIfText(validation, "maximum", DateMaximum);
                break;
        }

        return JsonSerializer.Serialize(validation);
    }

    public static ContractFieldMetadataInput FromEntity(ContractFieldMetadataDefinition entity)
    {
        var input = new ContractFieldMetadataInput
        {
            Name = entity.Name,
            Key = entity.Key,
            Description = entity.Description,
            DataType = "string",
            IsActive = entity.IsActive
        };

        return input;
    }

    private static void HydrateValidation(ContractFieldMetadataInput input, string validationJson)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(validationJson) ? "{}" : validationJson);
        var root = document.RootElement;

        if (root.TryGetProperty("minLength", out var minLength) && minLength.TryGetInt32(out var minLengthValue)) input.MinLength = minLengthValue;
        if (root.TryGetProperty("maxLength", out var maxLength) && maxLength.TryGetInt32(out var maxLengthValue)) input.MaxLength = maxLengthValue;
        if (root.TryGetProperty("pattern", out var pattern) && pattern.ValueKind == JsonValueKind.String) input.Pattern = pattern.GetString();
        if (root.TryGetProperty("minimum", out var minimum) && minimum.TryGetDecimal(out var minimumValue)) input.Minimum = minimumValue;
        if (root.TryGetProperty("maximum", out var maximum) && maximum.TryGetDecimal(out var maximumValue)) input.Maximum = maximumValue;
        if (input.DataType == "date" && root.TryGetProperty("minimum", out var dateMinimum) && dateMinimum.ValueKind == JsonValueKind.String) input.DateMinimum = dateMinimum.GetString();
        if (input.DataType == "date" && root.TryGetProperty("maximum", out var dateMaximum) && dateMaximum.ValueKind == JsonValueKind.String) input.DateMaximum = dateMaximum.GetString();
        if (root.TryGetProperty("allowedValues", out var allowedValues) && allowedValues.ValueKind == JsonValueKind.Array)
        {
            input.AllowedValuesText = string.Join(Environment.NewLine, allowedValues.EnumerateArray().Select(x => x.ToString()));
        }
    }

    private string[] ParseAllowedValues()
    {
        return (AllowedValuesText ?? string.Empty)
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArray();
    }

    private static void AddIfValue<T>(IDictionary<string, object?> target, string key, T? value) where T : struct
    {
        if (value.HasValue)
        {
            target[key] = value.Value;
        }
    }

    private static void AddIfText(IDictionary<string, object?> target, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            target[key] = value.Trim();
        }
    }

    private static void AddAllowedValues(IDictionary<string, object?> target, string[] values)
    {
        if (values.Length > 0)
        {
            target["allowedValues"] = values;
        }
    }
}

