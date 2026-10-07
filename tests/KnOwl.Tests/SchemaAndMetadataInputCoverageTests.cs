using System.Reflection;
using System.Text.Json;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.WebUI.ContractMetadata;
using KnOwl.ControlPlane.WebUI.SchemaTypes;

namespace KnOwl.Tests;

public sealed class SchemaAndMetadataInputCoverageTests
{
    [Fact]
    public void SchemaTypeBuildCreatesStringSchemaWithConstraintsAndUniqueEnum()
    {
        using var schema = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "string",
            MinLength = 2,
            MaxLength = 12,
            Pattern = " ^[A-Z]+$ ",
            AllowedValuesJson = """[" A ","A","B",""]"""
        }));

        Assert.Equal("string", schema.RootElement.GetProperty("type").GetString());
        Assert.Equal(2, schema.RootElement.GetProperty("minLength").GetInt32());
        Assert.Equal(12, schema.RootElement.GetProperty("maxLength").GetInt32());
        Assert.Equal("^[A-Z]+$", schema.RootElement.GetProperty("pattern").GetString());
        var allowedValues = schema.RootElement.GetProperty("enum").EnumerateArray().Select(x => x.GetString()).OfType<string>().ToArray();
        Assert.Equal(["A", "B"], allowedValues);
    }

    [Fact]
    public void SchemaTypeBuildCreatesIntegerSchemaWithBoundsAndUniqueEnum()
    {
        using var schema = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "integer",
            Minimum = 1,
            Maximum = 9,
            AllowedValuesJson = "[1,2,2,3]"
        }));

        Assert.Equal("integer", schema.RootElement.GetProperty("type").GetString());
        Assert.Equal(1, schema.RootElement.GetProperty("minimum").GetDecimal());
        Assert.Equal(9, schema.RootElement.GetProperty("maximum").GetDecimal());
        Assert.Equal([1, 2, 3], schema.RootElement.GetProperty("enum").EnumerateArray().Select(x => x.GetInt32()).ToArray());
    }

    [Fact]
    public void SchemaTypeBuildCreatesNumberSchemaWithPrecisionScaleAndUniqueEnum()
    {
        using var schema = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "number",
            Precision = 8,
            Scale = 2,
            Minimum = 1.5m,
            Maximum = 9.5m,
            AllowedValuesJson = "[1.5,2.25,2.25]"
        }));

        Assert.Equal("number", schema.RootElement.GetProperty("type").GetString());
        Assert.Equal(8, schema.RootElement.GetProperty("precision").GetInt32());
        Assert.Equal(2, schema.RootElement.GetProperty("scale").GetInt32());
        Assert.Equal([1.5m, 2.25m], schema.RootElement.GetProperty("enum").EnumerateArray().Select(x => x.GetDecimal()).ToArray());
    }

    [Fact]
    public void SchemaTypeBuildCreatesArrayReferencesAndNormalizesRequiredFields()
    {
        var definition = new SchemaTypeDefinition
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "Customer",
            Key = "customer"
        };
        var version = new SchemaTypeVersion
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
            SchemaTypeDefinitionId = definition.Id,
            SchemaTypeDefinition = definition,
            VersionNumber = "1.2.3",
            DefinitionJson = """
            {
              "schema": {
                "type": "object",
                "properties": {
                  "id": { "type": "string" },
                  "nested": {
                    "type": "object",
                    "properties": { "value": { "type": "string" } },
                    "required": ["value"]
                  }
                },
                "required": ["id", "nested"],
                "$defs": {
                  "Money": {
                    "type": "object",
                    "properties": { "amount": { "type": "number" } },
                    "required": ["amount"]
                  }
                }
              }
            }
            """
        };

        var schema = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "array",
            ArrayItemType = "object",
            ArrayItemTypeVersionId = version.Id,
            MinItems = 1,
            MaxItems = 5
        }, version));

        var root = schema.RootElement;
        Assert.Equal("array", root.GetProperty("type").GetString());
        Assert.Equal(1, root.GetProperty("minItems").GetInt32());
        Assert.Equal(5, root.GetProperty("maxItems").GetInt32());
        Assert.Equal("#/$defs/Customer@1.2.3", root.GetProperty("items").GetProperty("$ref").GetString());
        Assert.Equal(version.Id, root.GetProperty("items").GetProperty("typeVersionId").GetGuid());
        Assert.True(root.GetProperty("$defs").GetProperty("Customer@1.2.3").GetProperty("properties").GetProperty("id").GetProperty("required").GetBoolean());
        Assert.False(root.GetProperty("$defs").GetProperty("Customer@1.2.3").TryGetProperty("required", out _));
        Assert.True(root.GetProperty("$defs").GetProperty("Money").GetProperty("properties").GetProperty("amount").GetProperty("required").GetBoolean());
    }

    [Fact]
    public void SchemaTypeHydrateRestoresArrayItemVersionReference()
    {
        var input = new TypeVersionInput();

        SchemaTypeSchemaBuilder.Hydrate(input, """
        {
          "type": "array",
          "minItems": 2,
          "maxItems": 4,
          "items": { "typeVersionId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" },
          "enum": ["ignored"]
        }
        """);

        Assert.Equal("array", input.BaseType);
        Assert.Equal(2, input.MinItems);
        Assert.Equal(4, input.MaxItems);
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), input.ArrayItemTypeVersionId);
        Assert.Equal("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", input.ArrayItemType);
    }

    [Fact]
    public void SchemaTypeValidateRejectsInvalidArrayConfiguration()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "array",
            ArrayItemType = "not-a-type",
            MinItems = 5,
            MaxItems = 1,
            AllowedValuesJson = "[1]"
        });

        Assert.Contains("Select a valid array item type.", errors);
        Assert.Contains("MinItems no puede ser mayor que MaxItems.", errors);
        Assert.Contains("La lista de valores solo aplica para string, number o integer.", errors);
    }

    [Fact]
    public void SchemaTypeValidateRejectsInvalidNumberConfiguration()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "integer",
            AllowedValuesJson = """["not-number"]""",
            Precision = 0,
            Scale = -1
        });

        Assert.Contains("La lista de valores para number/integer solo acepta numeros.", errors);
        Assert.Contains("Precision debe ser mayor que cero.", errors);
        Assert.Contains("Scale no puede ser negativo.", errors);
    }

    [Fact]
    public void SchemaTypeValidateRejectsObjectSchemaWithWrongRootType()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "object",
            PayloadSchemaJson = """{"type":"array"}"""
        });

        Assert.Contains("El schema debe tener type 'object'.", errors);
    }

    [Fact]
    public void SchemaTypeBuildCreatesArrayWithBasicItemType()
    {
        using var arrayWithBasicItem = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "array",
            ArrayItemType = "string",
            AllowedValuesJson = "[]"
        }));
        Assert.Equal("string", arrayWithBasicItem.RootElement.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void SchemaTypeBuildFallsBackWhenReferencedDefinitionJsonIsInvalid()
    {
        var brokenVersion = new SchemaTypeVersion
        {
            Id = Guid.NewGuid(),
            SchemaTypeDefinitionId = Guid.NewGuid(),
            SchemaTypeDefinition = new SchemaTypeDefinition { Name = "Broken", Key = "broken" },
            VersionNumber = "preview",
            DefinitionJson = "{not-json"
        };
        using var arrayWithBrokenRef = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "array",
            ArrayItemTypeVersionId = brokenVersion.Id
        }, brokenVersion));
        Assert.Equal("{}", arrayWithBrokenRef.RootElement.GetProperty("$defs").GetProperty("Broken@preview").GetRawText());
    }

    [Fact]
    public void SchemaTypeHydrateRestoresArrayBasicItemType()
    {
        var hydrated = new TypeVersionInput();
        SchemaTypeSchemaBuilder.Hydrate(hydrated, """
        {
          "type": "array",
          "items": { "type": "integer" }
        }
        """);
        Assert.Equal("integer", hydrated.ArrayItemType);
    }

    [Fact]
    public void SchemaTypeValidateRejectsUnknownBaseType()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "bad",
            AllowedValuesJson = "[]"
        });
        Assert.Contains("Invalid base type.", errors);
    }

    [Fact]
    public void SchemaTypeValidateRejectsStringEnumShapeAndNegativeLengths()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "string",
            AllowedValuesJson = "{}",
            MinLength = -1,
            MaxLength = -2
        });
        Assert.Contains("La lista de valores debe ser un arreglo JSON.", errors);
        Assert.Contains("MinLength no puede ser negativo.", errors);
        Assert.Contains("MaxLength no puede ser negativo.", errors);
    }

    [Fact]
    public void SchemaTypeValidateRejectsInvalidValuesJson()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "string",
            AllowedValuesJson = "{"
        });
        Assert.Contains("The values list is not valid JSON.", errors);
    }

    [Fact]
    public void SchemaTypeValidateRejectsIntegerDecimalEnumAndNegativeArrayBounds()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "integer",
            AllowedValuesJson = "[1.2]",
            Minimum = 10,
            Maximum = 1,
            MinItems = -1,
            MaxItems = -2
        });
        Assert.Contains("La lista de valores para integer solo acepta enteros.", errors);
        Assert.Contains("MinItems no puede ser negativo.", errors);
        Assert.Contains("MaxItems no puede ser negativo.", errors);
    }

    [Fact]
    public void SchemaTypeValidateRejectsInvalidObjectJson()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "object",
            PayloadSchemaJson = "{"
        });
        Assert.Contains("The JSON schema is not valid.", errors);
    }

    [Fact]
    public void SchemaTypeValidateAcceptsValidObjectSchema()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "object",
            PayloadSchemaJson = """{"type":"object"}"""
        });
        Assert.Empty(errors);
    }

    [Fact]
    public void SchemaTypeValidateRejectsNumericEnumForString()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "string",
            AllowedValuesJson = "[1]"
        });
        Assert.Contains("La lista de valores para string solo acepta texto.", errors);
    }

    [Fact]
    public void SchemaTypeValidateAcceptsIntegerEnum()
    {
        var errors = ValidateSchema(new TypeVersionInput
        {
            BaseType = "integer",
            AllowedValuesJson = "[1]"
        });
        Assert.Empty(errors);
    }

    [Fact]
    public void SchemaTypeBuildCreatesObjectSchemaFromPayloadJson()
    {
        using var objectSchema = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "object",
            PayloadSchemaJson = """{"type":"object"}"""
        }));
        Assert.Equal("object", objectSchema.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public void SchemaTypeBuildOmitsEnumForBlankOrNonArrayValues()
    {
        using var emptyEnumSchema = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "string",
            AllowedValuesJson = " "
        }));
        Assert.False(emptyEnumSchema.RootElement.TryGetProperty("enum", out _));

        using var nonArrayEnumSchema = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "string",
            AllowedValuesJson = "{}"
        }));
        Assert.False(nonArrayEnumSchema.RootElement.TryGetProperty("enum", out _));
    }

    [Fact]
    public void SchemaTypeBuildFallsBackWhenReferencedSchemaHasNoSchemaNode()
    {
        var missingSchemaVersion = new SchemaTypeVersion
        {
            Id = Guid.NewGuid(),
            SchemaTypeDefinitionId = Guid.NewGuid(),
            SchemaTypeDefinition = new SchemaTypeDefinition { Name = "Fallback", Key = "fallback" },
            VersionNumber = "1.0.0",
            DefinitionJson = "{}"
        };
        using var arrayWithMissingSchemaRef = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "array",
            ArrayItemTypeVersionId = missingSchemaVersion.Id
        }, missingSchemaVersion));
        Assert.Equal("{}", arrayWithMissingSchemaRef.RootElement.GetProperty("$defs").GetProperty("Fallback@1.0.0").GetRawText());
    }

    [Fact]
    public void MetadataInputBuildsAppliesToJsonForEvents()
    {
        var input = new ContractFieldMetadataInput
        {
            AppliesToEvents = true,
            AppliesToCommands = false,
            DataType = "string"
        };

        Assert.Equal("""["events"]""", input.BuildAppliesToJson());
    }

    [Fact]
    public void MetadataInputBuildsStringValidationJson()
    {
        var input = new ContractFieldMetadataInput
        {
            DataType = "string",
            MinLength = 1,
            MaxLength = 20,
            Pattern = " ^A ",
            AllowedValuesText = "alpha\nalpha\n beta "
        };

        using var stringValidation = Read(input.BuildValidationJson());
        Assert.Equal(1, stringValidation.RootElement.GetProperty("minLength").GetInt32());
        Assert.Equal(20, stringValidation.RootElement.GetProperty("maxLength").GetInt32());
        Assert.Equal("^A", stringValidation.RootElement.GetProperty("pattern").GetString());
        var fieldAllowedValues = stringValidation.RootElement.GetProperty("allowedValues").EnumerateArray().Select(x => x.GetString()).OfType<string>().ToArray();
        Assert.Equal(["alpha", "beta"], fieldAllowedValues);
    }

    [Fact]
    public void MetadataInputBuildsNumberValidationJson()
    {
        var input = new ContractFieldMetadataInput
        {
            DataType = "number",
            Minimum = 1.2m,
            Maximum = 9.8m
        };

        using var validation = Read(input.BuildValidationJson());

        Assert.Equal(1.2m, validation.RootElement.GetProperty("minimum").GetDecimal());
        Assert.Equal(9.8m, validation.RootElement.GetProperty("maximum").GetDecimal());
    }

    [Fact]
    public void MetadataInputBuildsDateValidationJson()
    {
        var input = new ContractFieldMetadataInput
        {
            DataType = "date",
            DateMinimum = "2026-01-01",
            DateMaximum = "2026-12-31"
        };

        using var validation = Read(input.BuildValidationJson());

        Assert.Equal("2026-01-01", validation.RootElement.GetProperty("minimum").GetString());
        Assert.Equal("2026-12-31", validation.RootElement.GetProperty("maximum").GetString());
    }

    [Fact]
    public void MetadataInputBuildsEmptyValidationForBoolean()
    {
        var input = new ContractFieldMetadataInput { DataType = "boolean" };

        using var validation = Read(input.BuildValidationJson());

        Assert.Empty(validation.RootElement.EnumerateObject());
    }

    [Fact]
    public void MetadataInputValidateReportsInvalidShapeErrors()
    {
        var errors = ValidateMetadata(new ContractFieldMetadataInput
        {
            DataType = "bad",
            AppliesToEvents = false,
            AppliesToCommands = false,
            MinLength = -1,
            MaxLength = -2,
            Minimum = 5,
            Maximum = 4,
            DateMinimum = "bad-date",
            DateMaximum = "2026-01-01"
        });

        Assert.Contains("Invalid data type.", errors);
        Assert.Contains("Select at least one applicable section.", errors);
        Assert.Contains("MinLength no puede ser negativo.", errors);
        Assert.Contains("MaxLength no puede ser negativo.", errors);
        Assert.Contains("Minimum no puede ser mayor que Maximum.", errors);
        Assert.Contains("Fecha minima invalida.", errors);
    }

    [Fact]
    public void MetadataInputValidateRejectsInvertedDateRange()
    {
        var errors = ValidateMetadata(new ContractFieldMetadataInput
        {
            DataType = "date",
            DateMinimum = "2026-12-31",
            DateMaximum = "2026-01-01"
        });

        Assert.Contains("Fecha minima no puede ser mayor que fecha maxima.", errors);
    }

    [Fact]
    public void MetadataInputFromEntityCopiesDefinitionFields()
    {
        var entity = new ContractFieldMetadataDefinition
        {
            Name = "Trace Id",
            Key = "trace-id",
            Description = "Correlation",
            IsActive = false
        };
        var mapped = ContractFieldMetadataInput.FromEntity(entity);
        Assert.Equal(entity.Name, mapped.Name);
        Assert.Equal(entity.Key, mapped.Key);
        Assert.Equal(entity.Description, mapped.Description);
        Assert.False(mapped.IsActive);
    }

    [Fact]
    public void MetadataInputHydrateValidationRestoresDateBoundsAndAllowedValues()
    {
        var hydrated = new ContractFieldMetadataInput { DataType = "date" };
        typeof(ContractFieldMetadataInput)
            .GetMethod("HydrateValidation", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [hydrated, """{"minimum":"2026-01-02","maximum":"2026-03-04","allowedValues":["a","b"]}"""]);

        Assert.Equal("2026-01-02", hydrated.DateMinimum);
        Assert.Equal("2026-03-04", hydrated.DateMaximum);
        Assert.Equal($"a{Environment.NewLine}b", hydrated.AllowedValuesText);
    }

    private static List<string> ValidateSchema(TypeVersionInput input)
    {
        var errors = new List<string>();
        SchemaTypeSchemaBuilder.Validate(input, errors.Add);
        return errors;
    }

    private static List<string> ValidateMetadata(ContractFieldMetadataInput input)
    {
        var errors = new List<string>();
        input.Validate(errors.Add);
        return errors;
    }

    private static JsonDocument Read(string json) => JsonDocument.Parse(json);
}
