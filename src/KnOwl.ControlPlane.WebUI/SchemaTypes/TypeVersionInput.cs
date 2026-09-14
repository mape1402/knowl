using System.ComponentModel.DataAnnotations;

namespace KnOwl.ControlPlane.WebUI.SchemaTypes;

public class TypeVersionInput
{
    [Required]
    [MaxLength(13)]
    [Display(Name = "Version")]
    public string VersionNumber { get; set; } = "1.0.0";

    [Required]
    [MaxLength(20)]
    [Display(Name = "Type base")]
    public string BaseType { get; set; } = "string";

    [MaxLength(1000)]
    [Display(Name = "Version Comment")]
    public string? Comment { get; set; }

    [Display(Name = "Min length")]
    public int? MinLength { get; set; }

    [Display(Name = "Max length")]
    public int? MaxLength { get; set; }

    [Display(Name = "Regex")]
    public string? Pattern { get; set; }

    public string AllowedValuesJson { get; set; } = "[]";

    [Display(Name = "Precision")]
    public int? Precision { get; set; }

    [Display(Name = "Scale")]
    public int? Scale { get; set; }

    [Display(Name = "Minimum")]
    public decimal? Minimum { get; set; }

    [Display(Name = "Maximum")]
    public decimal? Maximum { get; set; }

    [Display(Name = "Min items")]
    public int? MinItems { get; set; }

    [Display(Name = "Max items")]
    public int? MaxItems { get; set; }

    public string ArrayItemType { get; set; } = "string";

    public Guid? ArrayItemTypeVersionId { get; set; }

    public string PayloadSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
}

