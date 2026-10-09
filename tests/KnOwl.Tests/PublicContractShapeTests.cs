using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Api.Contracts;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Documentation;
using KnOwl.Documentation.Api;
using KnOwl.Documentation.Application;
using KnOwl.Runtime.Api.Contracts;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Distribution;
using KnOwl.WolfAuth;

namespace KnOwl.Tests;

public sealed class PublicContractShapeTests
{
    private static readonly Assembly[] TargetAssemblies =
    [
        typeof(ContractArtifact).Assembly,
        typeof(RuntimeArtifactDeliveryPackage).Assembly,
        typeof(SchemaTypeDefinition).Assembly,
        typeof(RuntimeNodeCredentialPackageModel).Assembly,
        typeof(SchemaTypeResponse).Assembly,
        typeof(RuntimeContractArtifact).Assembly,
        typeof(RuntimeDesignNodeCredentialPackageModel).Assembly,
        typeof(RuntimeStatusResponse).Assembly,
        typeof(DocumentationSpace).Assembly,
        typeof(DocumentationVersionInput).Assembly,
        typeof(DocumentationSpaceResponse).Assembly,
        typeof(KnOwlWolfAuthOptions).Assembly
    ];

    public static IEnumerable<object[]> PublicMutableProperties()
        => TargetTypes()
            .SelectMany(type => WritablePublicProperties(type)
                .Where(property => CanCreateInstance(type) && TryCreateSample(property.PropertyType, property.Name, out _))
                .Select(property => new object[] { type.Assembly.GetName().Name!, type.FullName!, property.Name }));

    public static IEnumerable<object[]> ConstructorMappedProperties()
        => TargetTypes()
            .SelectMany(type => PublicConstructors(type)
                .SelectMany(ctor => ctor.GetParameters()
                    .Where(parameter => TryCreateSample(parameter.ParameterType, parameter.Name ?? type.Name, out _))
                    .Select(parameter => new { Constructor = ctor, Parameter = parameter, Property = FindProperty(type, parameter) })
                    .Where(match => match.Property is not null)
                    .Select(match => new object[] { type.Assembly.GetName().Name!, type.FullName!, ConstructorSignature(match.Constructor), match.Parameter.Name!, match.Property!.Name })));

    public static IEnumerable<object[]> RequiredProperties()
        => TargetTypes()
            .SelectMany(type => WritablePublicProperties(type)
                .Where(property => CanCreateInstance(type))
                .Where(property => property.GetCustomAttribute<RequiredAttribute>() is not null)
                .Where(property => !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) is not null)
                .Select(property => new object[] { type.Assembly.GetName().Name!, type.FullName!, property.Name }));

    public static IEnumerable<object[]> MaxLengthStringProperties()
        => TargetTypes()
            .SelectMany(type => WritablePublicProperties(type)
                .Where(property => CanCreateInstance(type))
                .Where(property => property.PropertyType == typeof(string))
                .Select(property => new { Property = property, Attribute = property.GetCustomAttribute<MaxLengthAttribute>() })
                .Where(match => match.Attribute is not null && match.Attribute.Length > 0)
                .Select(match => new object[] { type.Assembly.GetName().Name!, type.FullName!, match.Property.Name, match.Attribute!.Length }));

    [Theory]
    [MemberData(nameof(PublicMutableProperties))]
    public void PublicMutablePropertyRoundTripsItsAssignedValue(string assemblyName, string typeName, string propertyName)
    {
        var type = ResolveType(assemblyName, typeName);
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;
        var instance = CreateInstance(type);
        Assert.True(TryCreateSample(property.PropertyType, property.Name, out var value));

        property.SetValue(instance, value);

        AssertPropertyValue(value, property.GetValue(instance));
    }

    [Theory]
    [MemberData(nameof(ConstructorMappedProperties))]
    public void PublicConstructorArgumentInitializesMatchingProperty(
        string assemblyName,
        string typeName,
        string constructorSignature,
        string parameterName,
        string propertyName)
    {
        var type = ResolveType(assemblyName, typeName);
        var constructor = PublicConstructors(type).Single(ctor => ConstructorSignature(ctor) == constructorSignature);
        var parameters = constructor.GetParameters();
        var values = parameters.Select(parameter =>
        {
            Assert.True(TryCreateSample(parameter.ParameterType, parameter.Name ?? type.Name, out var value));
            return value;
        }).ToArray();
        var parameterIndex = Array.FindIndex(parameters, parameter => string.Equals(parameter.Name, parameterName, StringComparison.Ordinal));
        var expected = values[parameterIndex];

        var instance = constructor.Invoke(values);

        AssertPropertyValue(expected, type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance));
    }

    [Theory]
    [MemberData(nameof(RequiredProperties))]
    public void RequiredPropertyRejectsMissingValue(string assemblyName, string typeName, string propertyName)
    {
        var type = ResolveType(assemblyName, typeName);
        var instance = CreateValidatableInstance(type);
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;

        property.SetValue(instance, null);

        Assert.Contains(Validate(instance), result => result.MemberNames.Contains(propertyName));
    }

    [Theory]
    [MemberData(nameof(MaxLengthStringProperties))]
    public void MaxLengthPropertyRejectsOversizedValue(string assemblyName, string typeName, string propertyName, int maxLength)
    {
        var type = ResolveType(assemblyName, typeName);
        var instance = CreateValidatableInstance(type);
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;

        property.SetValue(instance, new string('x', maxLength + 1));

        Assert.Contains(Validate(instance), result => result.MemberNames.Contains(propertyName));
    }

    private static IEnumerable<Type> TargetTypes()
        => TargetAssemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.Namespace?.StartsWith("KnOwl.", StringComparison.Ordinal) == true)
            .Where(type => !type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition)
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

    private static IEnumerable<PropertyInfo> WritablePublicProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0)
            .Where(property => property.GetMethod is not null && property.SetMethod is not null)
            .OrderBy(property => property.Name, StringComparer.Ordinal);

    private static IEnumerable<ConstructorInfo> PublicConstructors(Type type)
        => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Where(ctor => ctor.GetParameters().Length > 0)
            .Where(ctor => ctor.GetParameters().All(parameter => TryCreateSample(parameter.ParameterType, parameter.Name ?? type.Name, out _)))
            .OrderBy(ctor => ConstructorSignature(ctor), StringComparer.Ordinal);

    private static bool CanCreateInstance(Type type)
        => type.GetConstructor(Type.EmptyTypes) is not null
           || PublicConstructors(type).Any();

    private static object CreateInstance(Type type)
    {
        var defaultConstructor = type.GetConstructor(Type.EmptyTypes);
        if (defaultConstructor is not null)
        {
            return defaultConstructor.Invoke([]);
        }

        var constructor = PublicConstructors(type).OrderBy(ctor => ctor.GetParameters().Length).First();
        var values = constructor.GetParameters()
            .Select(parameter =>
            {
                TryCreateSample(parameter.ParameterType, parameter.Name ?? type.Name, out var value);
                return value;
            })
            .ToArray();

        return constructor.Invoke(values);
    }

    private static object CreateValidatableInstance(Type type)
    {
        var instance = CreateInstance(type);
        foreach (var property in WritablePublicProperties(type))
        {
            if (TryCreateSample(property.PropertyType, property.Name, out var value))
            {
                property.SetValue(instance, value);
            }
        }

        return instance;
    }

    private static Type ResolveType(string assemblyName, string typeName)
        => TargetAssemblies
            .Distinct()
            .Single(assembly => assembly.GetName().Name == assemblyName)
            .GetType(typeName, throwOnError: true)!;

    private static PropertyInfo? FindProperty(Type type, ParameterInfo parameter)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(property => string.Equals(property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));

    private static string ConstructorSignature(ConstructorInfo constructor)
        => string.Join("|", constructor.GetParameters().Select(parameter => $"{parameter.ParameterType.FullName}:{parameter.Name}"));

    private static bool TryCreateSample(Type type, string seed, out object? value)
    {
        var nullableType = Nullable.GetUnderlyingType(type);
        var targetType = nullableType ?? type;

        if (targetType == typeof(string))
        {
            value = $"sample-{seed}";
            return true;
        }

        if (targetType == typeof(Guid))
        {
            value = Guid.Parse("11111111-1111-1111-1111-111111111111");
            return true;
        }

        if (targetType == typeof(DateTime))
        {
            value = new DateTime(2026, 9, 18, 20, 7, 0, DateTimeKind.Utc);
            return true;
        }

        if (targetType == typeof(DateTimeOffset))
        {
            value = new DateTimeOffset(2026, 9, 18, 20, 7, 0, TimeSpan.Zero);
            return true;
        }

        if (targetType == typeof(TimeSpan))
        {
            value = TimeSpan.FromMinutes(7);
            return true;
        }

        if (targetType == typeof(bool))
        {
            value = true;
            return true;
        }

        if (targetType == typeof(int))
        {
            value = 7;
            return true;
        }

        if (targetType == typeof(long))
        {
            value = 7L;
            return true;
        }

        if (targetType == typeof(decimal))
        {
            value = 7.5m;
            return true;
        }

        if (targetType == typeof(double))
        {
            value = 7.5d;
            return true;
        }

        if (targetType == typeof(float))
        {
            value = 7.5f;
            return true;
        }

        if (targetType == typeof(byte[]))
        {
            value = new byte[] { 1, 2, 3 };
            return true;
        }

        if (targetType.IsEnum)
        {
            value = Enum.GetValues(targetType).Cast<object>().First();
            return true;
        }

        if (targetType.IsArray && TryCreateSample(targetType.GetElementType()!, seed, out var item))
        {
            var array = Array.CreateInstance(targetType.GetElementType()!, 1);
            array.SetValue(item, 0);
            value = array;
            return true;
        }

        if (TryCreateGenericList(targetType, seed, out value))
        {
            return true;
        }

        if (targetType == typeof(object))
        {
            value = $"sample-{seed}";
            return true;
        }

        if (!targetType.IsAbstract && !targetType.IsInterface && !targetType.IsGenericTypeDefinition && CanCreateInstance(targetType))
        {
            value = CreateInstance(targetType);
            return true;
        }

        value = null;
        return nullableType is not null;
    }

    private static bool TryCreateGenericList(Type type, string seed, out object? value)
    {
        var enumerableType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableType is null || type == typeof(string))
        {
            value = null;
            return false;
        }

        var itemType = enumerableType.GetGenericArguments()[0];
        if (!TryCreateSample(itemType, seed, out var item))
        {
            value = null;
            return false;
        }

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
        list.Add(item);

        if (type.IsAssignableFrom(list.GetType()))
        {
            value = list;
            return true;
        }

        value = null;
        return false;
    }

    private static void AssertPropertyValue(object? expected, object? actual)
    {
        if (expected is Array expectedArray && actual is Array actualArray)
        {
            Assert.Equal(expectedArray.Cast<object?>(), actualArray.Cast<object?>());
            return;
        }

        if (expected is IEnumerable expectedItems && expected is not string && actual is IEnumerable actualItems && actual is not string)
        {
            Assert.Equal(expectedItems.Cast<object?>(), actualItems.Cast<object?>());
            return;
        }

        Assert.Equal(expected, actual);
    }

    private static IReadOnlyList<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
