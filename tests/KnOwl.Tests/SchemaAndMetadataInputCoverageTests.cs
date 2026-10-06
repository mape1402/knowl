using System.Reflection;
using System.Text.Json;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.WebUI.ContractMetadata;
using KnOwl.ControlPlane.WebUI.SchemaTypes;

namespace KnOwl.Tests;

public sealed class SchemaAndMetadataInputCoverageTests
{
    [Fact]
    public void SchemaTypeBuildCreatesScalarSchemasWithConstraintsAndEnums()
    {
        var stringSchema = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "string",
            MinLength = 2,
            MaxLength = 12,
            Pattern = " ^[A-Z]+$ ",
            AllowedValuesJson = """[" A ","A","B",""]"""
        }));

        Assert.Equal("string", stringSchema.RootElement.GetProperty("type").GetString());
        Assert.Equal(2, stringSchema.RootElement.GetProperty("minLength").GetInt32());
        Assert.Equal(12, stringSchema.RootElement.GetProperty("maxLength").GetInt32());
        Assert.Equal("^[A-Z]+$", stringSchema.RootElement.GetProperty("pattern").GetString());
        Assert.Equal(["A", "B"], stringSchema.RootElement.GetProperty("enum").EnumerateArray().Select(x => x.GetString()).ToArray());

        var integerSchema = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "integer",
            Minimum = 1,
            Maximum = 9,
            AllowedValuesJson = "[1,2,2,3]"
        }));

        Assert.Equal("integer", integerSchema.RootElement.GetProperty("type").GetString());
        Assert.Equal(1, integerSchema.RootElement.GetProperty("minimum").GetDecimal());
        Assert.Equal(9, integerSchema.RootElement.GetProperty("maximum").GetDecimal());
        Assert.Equal([1, 2, 3], integerSchema.RootElement.GetProperty("enum").EnumerateArray().Select(x => x.GetInt32()).ToArray());

        var numberSchema = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "number",
            Precision = 8,
            Scale = 2,
            Minimum = 1.5m,
            Maximum = 9.5m,
            AllowedValuesJson = "[1.5,2.25,2.25]"
        }));

        Assert.Equal(8, numberSchema.RootElement.GetProperty("precision").GetInt32());
        Assert.Equal(2, numberSchema.RootElement.GetProperty("scale").GetInt32());
        Assert.Equal([1.5m, 2.25m], numberSchema.RootElement.GetProperty("enum").EnumerateArray().Select(x => x.GetDecimal()).ToArray());
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
    public void SchemaTypeHydrateAndValidateCoverValidAndInvalidInputs()
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

        var errors = new List<string>();
        SchemaTypeSchemaBuilder.Validate(new TypeVersionInput
        {
            BaseType = "array",
            ArrayItemType = "not-a-type",
            MinItems = 5,
            MaxItems = 1,
            AllowedValuesJson = "[1]"
        }, errors.Add);

        Assert.Contains("Select a valid array item type.", errors);
        Assert.Contains("MinItems no puede ser mayor que MaxItems.", errors);
        Assert.Contains("La lista de valores solo aplica para string, number o integer.", errors);

        errors.Clear();
        SchemaTypeSchemaBuilder.Validate(new TypeVersionInput
        {
            BaseType = "integer",
            AllowedValuesJson = """["not-number"]""",
            Precision = 0,
            Scale = -1
        }, errors.Add);

        Assert.Contains("La lista de valores para number/integer solo acepta numeros.", errors);
        Assert.Contains("Precision debe ser mayor que cero.", errors);
        Assert.Contains("Scale no puede ser negativo.", errors);

        errors.Clear();
        SchemaTypeSchemaBuilder.Validate(new TypeVersionInput
        {
            BaseType = "object",
            PayloadSchemaJson = """{"type":"array"}"""
        }, errors.Add);

        Assert.Contains("El schema debe tener type 'object'.", errors);
    }

    [Fact]
    public void SchemaTypeBuilderCoversRemainingEnumArrayAndObjectValidationBranches()
    {
        using var arrayWithBasicItem = Read(SchemaTypeSchemaBuilder.Build(new TypeVersionInput
        {
            BaseType = "array",
            ArrayItemType = "string",
            AllowedValuesJson = "[]"
        }));
        Assert.Equal("string", arrayWithBasicItem.RootElement.GetProperty("items").GetProperty("type").GetString());

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

        var hydrated = new TypeVersionInput();
        SchemaTypeSchemaBuilder.Hydrate(hydrated, """
        {
          "type": "array",
          "items": { "type": "integer" }
        }
        """);
        Assert.Equal("integer", hydrated.ArrayItemType);

        var errors = new List<string>();
        SchemaTypeSchemaBuilder.Validate(new TypeVersionInput
        {
            BaseType = "bad",
            AllowedValuesJson = "[]"
        }, errors.Add);
        Assert.Contains("Invalid base type.", errors);

        errors.Clear();
        SchemaTypeSchemaBuilder.Validate(new TypeVersionInput
        {
            BaseType = "string",
            AllowedValuesJson = "{}",
            MinLength = -1,
            MaxLength = -2
        }, errors.Add);
        Assert.Contains("La lista de valores debe ser un arreglo JSON.", errors);
        Assert.Contains("MinLength no puede ser negativo.", errors);
        Assert.Contains("MaxLength no puede ser negativo.", errors);

        errors.Clear();
        SchemaTypeSchemaBuilder.Validate(new TypeVersionInput
        {
            BaseType = "string",
            AllowedValuesJson = "{"
        }, errors.Add);
        Assert.Contains("The values list is not valid JSON.", errors);

        errors.Clear();
        SchemaTypeSchemaBuilder.Validate(new TypeVersionInput
        {
            BaseType = "integer",
            AllowedValuesJson = "[1.2]",
            Minimum = 10,
            Maximum = 1,
            MinItems = -1,
            MaxItems = -2
        }, errors.Add);
        Assert.Contains("La lista de valores para integer solo acepta enteros.", errors);
        Assert.Contains("MinItems no puede ser negativo.", errors);
        Assert.Contains("MaxItems no puede ser negativo.", errors);

        errors.Clear();
        SchemaTypeSchemaBuilder.Validate(new TypeVersionInput
        {
            BaseType = "object",
            PayloadSchemaJson = "{"
        }, errors.Add);
        Assert.Contains("The JSON schema is not valid.", errors);
    }

    [Fact]
    public void MetadataInputBuildsValidationAndAppliesToJson()
    {
        var input = new ContractFieldMetadataInput
        {
            AppliesToEvents = true,
            AppliesToCommands = false,
            DataType = "string",
            MinLength = 1,
            MaxLength = 20,
            Pattern = " ^A ",
            AllowedValuesText = "alpha\nalpha\n beta "
        };

        Assert.Equal("""["events"]""", input.BuildAppliesToJson());
        using var stringValidation = Read(input.BuildValidationJson());
        Assert.Equal(1, stringValidation.RootElement.GetProperty("minLength").GetInt32());
        Assert.Equal(20, stringValidation.RootElement.GetProperty("maxLength").GetInt32());
        Assert.Equal("^A", stringValidation.RootElement.GetProperty("pattern").GetString());
        Assert.Equal(["alpha", "beta"], stringValidation.RootElement.GetProperty("allowedValues").EnumerateArray().Select(x => x.GetString()).ToArray());

        input.DataType = "number";
        input.Minimum = 1.2m;
        input.Maximum = 9.8m;
        using var numberValidation = Read(input.BuildValidationJson());
        Assert.Equal(1.2m, numberValidation.RootElement.GetProperty("minimum").GetDecimal());
        Assert.Equal(9.8m, numberValidation.RootElement.GetProperty("maximum").GetDecimal());

        input.DataType = "date";
        input.DateMinimum = "2026-01-01";
        input.DateMaximum = "2026-12-31";
        using var dateValidation = Read(input.BuildValidationJson());
        Assert.Equal("2026-01-01", dateValidation.RootElement.GetProperty("minimum").GetString());
        Assert.Equal("2026-12-31", dateValidation.RootElement.GetProperty("maximum").GetString());

        input.DataType = "boolean";
        using var booleanValidation = Read(input.BuildValidationJson());
        Assert.Empty(booleanValidation.RootElement.EnumerateObject());
    }

    [Fact]
    public void MetadataInputValidateHydrateAndEntityMappingCoverEdgeCases()
    {
        var errors = new List<string>();
        new ContractFieldMetadataInput
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
        }.Validate(errors.Add);

        Assert.Contains("Invalid data type.", errors);
        Assert.Contains("Select at least one applicable section.", errors);
        Assert.Contains("MinLength no puede ser negativo.", errors);
        Assert.Contains("MaxLength no puede ser negativo.", errors);
        Assert.Contains("Minimum no puede ser mayor que Maximum.", errors);
        Assert.Contains("Fecha minima invalida.", errors);

        errors.Clear();
        new ContractFieldMetadataInput
        {
            DataType = "date",
            DateMinimum = "2026-12-31",
            DateMaximum = "2026-01-01"
        }.Validate(errors.Add);

        Assert.Contains("Fecha minima no puede ser mayor que fecha maxima.", errors);

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

        var hydrated = new ContractFieldMetadataInput { DataType = "date" };
        typeof(ContractFieldMetadataInput)
            .GetMethod("HydrateValidation", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [hydrated, """{"minimum":"2026-01-02","maximum":"2026-03-04","allowedValues":["a","b"]}"""]);

        Assert.Equal("2026-01-02", hydrated.DateMinimum);
        Assert.Equal("2026-03-04", hydrated.DateMaximum);
        Assert.Equal($"a{Environment.NewLine}b", hydrated.AllowedValuesText);
    }

    private static JsonDocument Read(string json) => JsonDocument.Parse(json);
}
