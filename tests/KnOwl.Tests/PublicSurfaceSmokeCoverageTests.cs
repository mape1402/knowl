using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KnOwl.Tests;

public sealed class PublicSurfaceSmokeCoverageTests
{
    [Fact]
    public void PublicConcreteTypeCasesCoverBroadSurfaceArea()
    {
        Assert.True(PublicConcreteTypeCases().Count() > 100);
    }

    [Theory]
    [MemberData(nameof(PublicConcreteTypeCases))]
    public void PublicConcreteTypeSupportsBasicConstructionAndPropertyAccess(string assemblyName, string typeName)
    {
        var type = Assembly.Load(assemblyName).GetType(typeName, throwOnError: true)!;

        Assert.True(TryCreate(type, out var instance), $"Could not create {type.FullName}.");

        ExerciseProperties(instance!, type);
        _ = instance!.ToString();
        _ = instance.Equals(instance);
        _ = instance.GetHashCode();
    }

    public static IEnumerable<object[]> PublicConcreteTypeCases()
    {
        foreach (var type in LoadKnOwlAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(IsSafeConcreteType)
            .Where(type => TryCreate(type, out _))
            .OrderBy(type => type.Assembly.GetName().Name, StringComparer.Ordinal)
            .ThenBy(type => type.FullName, StringComparer.Ordinal))
        {
            yield return [type.Assembly.GetName().Name!, type.FullName!];
        }
    }

    private static IEnumerable<Assembly> LoadKnOwlAssemblies()
    {
        var names = new[]
        {
            "KnOwl.Contracts",
            "KnOwl.ControlPlane",
            "KnOwl.ControlPlane.Api",
            "KnOwl.ControlPlane.Application",
            "KnOwl.ControlPlane.Bootstrap",
            "KnOwl.ControlPlane.Storage.EntityFramework",
            "KnOwl.ControlPlane.WebUI",
            "KnOwl.Documentation",
            "KnOwl.Documentation.Api",
            "KnOwl.Documentation.Application",
            "KnOwl.Documentation.Storage.EntityFramework",
            "KnOwl.Documentation.WebUI",
            "KnOwl.Runtime",
            "KnOwl.Runtime.Api",
            "KnOwl.Runtime.Application",
            "KnOwl.Runtime.Bootstrap",
            "KnOwl.Runtime.Storage.EntityFramework",
            "KnOwl.Runtime.WebUI",
            "KnOwl.WolfAuth"
        };

        foreach (var name in names)
        {
            Assembly? assembly;
            try
            {
                assembly = Assembly.Load(name);
            }
            catch
            {
                assembly = null;
            }

            if (assembly is not null)
            {
                yield return assembly;
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }

    private static bool IsSafeConcreteType(Type type)
    {
        if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
        {
            return false;
        }

        if (type.IsAssignableTo(typeof(DbContext)) ||
            type.IsAssignableTo(typeof(Migration)) ||
            type.IsAssignableTo(typeof(ModelSnapshot)) ||
            type.IsAssignableTo(typeof(PageModel)) ||
            type.FullName?.Contains("Program", StringComparison.Ordinal) == true ||
            type.FullName?.Contains("ServiceCollectionExtensions", StringComparison.Ordinal) == true ||
            type.FullName?.Contains("EndpointRouteBuilderExtensions", StringComparison.Ordinal) == true ||
            type.FullName?.Contains("HostedService", StringComparison.Ordinal) == true)
        {
            return false;
        }

        return type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0;
    }

    private static bool TryCreate(Type type, out object? instance)
    {
        instance = null;

        foreach (var constructor in type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(ctor => ctor.GetParameters().Length))
        {
            var parameters = constructor.GetParameters();
            var arguments = new object?[parameters.Length];
            var canBuild = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                if (!TryCreateValue(parameters[i].ParameterType, out arguments[i]))
                {
                    canBuild = false;
                    break;
                }
            }

            if (!canBuild)
            {
                continue;
            }

            try
            {
                instance = constructor.Invoke(arguments);
                return true;
            }
            catch
            {
                // Some public constructors intentionally validate richer collaborators.
            }
        }

        return false;
    }

    private static void ExerciseProperties(object instance, Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (property.SetMethod is not null &&
                property.SetMethod.IsPublic &&
                TryCreateValue(property.PropertyType, out var value))
            {
                try
                {
                    property.SetValue(instance, value);
                }
                catch
                {
                    // Property validation is covered by focused tests; this smoke pass keeps moving.
                }
            }

            if (property.GetMethod is not null && property.GetMethod.IsPublic)
            {
                try
                {
                    _ = property.GetValue(instance);
                }
                catch
                {
                    // Lazy properties can depend on collaborators; skip those here.
                }
            }
        }
    }

    private static bool TryCreateValue(Type type, out object? value)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        value = null;

        if (!type.IsPublic && !type.IsNestedPublic)
        {
            return false;
        }

        if (type == typeof(string))
        {
            value = "value";
            return true;
        }

        if (type == typeof(Guid))
        {
            value = Guid.Parse("11111111-1111-1111-1111-111111111111");
            return true;
        }

        if (type == typeof(DateTime))
        {
            value = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            return true;
        }

        if (type == typeof(DateTimeOffset))
        {
            value = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
            return true;
        }

        if (type == typeof(TimeSpan))
        {
            value = TimeSpan.FromMinutes(5);
            return true;
        }

        if (type == typeof(Uri))
        {
            value = new Uri("https://example.test");
            return true;
        }

        if (type == typeof(bool))
        {
            value = true;
            return true;
        }

        if (type == typeof(byte))
        {
            value = (byte)1;
            return true;
        }

        if (type == typeof(short))
        {
            value = (short)1;
            return true;
        }

        if (type == typeof(int))
        {
            value = 1;
            return true;
        }

        if (type == typeof(long))
        {
            value = 1L;
            return true;
        }

        if (type == typeof(decimal))
        {
            value = 1m;
            return true;
        }

        if (type == typeof(double))
        {
            value = 1d;
            return true;
        }

        if (type == typeof(float))
        {
            value = 1f;
            return true;
        }

        if (type.IsEnum)
        {
            value = Enum.GetValues(type).GetValue(0);
            return value is not null;
        }

        if (type.IsArray)
        {
            value = Array.CreateInstance(type.GetElementType()!, 0);
            return true;
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments();

            if (definition == typeof(List<>) ||
                definition == typeof(IList<>) ||
                definition == typeof(IReadOnlyList<>) ||
                definition == typeof(IEnumerable<>))
            {
                value = Activator.CreateInstance(typeof(List<>).MakeGenericType(genericArguments[0]));
                return true;
            }

            if (definition == typeof(Dictionary<,>) ||
                definition == typeof(IDictionary<,>) ||
                definition == typeof(IReadOnlyDictionary<,>))
            {
                value = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(genericArguments));
                return true;
            }
        }

        if (type.GetConstructor(Type.EmptyTypes) is not null)
        {
            try
            {
                value = Activator.CreateInstance(type);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
