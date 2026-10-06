using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KnOwl.Tests;

public sealed class GeneratedMigrationCoverageTests
{
    [Fact]
    public void SampleHostMigrationsCanBuildOperationsAndTargetModels()
    {
        var assemblies = new[]
        {
            Assembly.Load("KnOwl.ControlPlaneHost.Sample"),
            Assembly.Load("KnOwl.RuntimeHost.Sample")
        };

        var migrations = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && typeof(Migration).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(migrations);

        foreach (var migrationType in migrations)
        {
            var migration = (Migration)Activator.CreateInstance(migrationType)!;

            InvokeMigrationBuilderMethod(migration, "Up");
            InvokeMigrationBuilderMethod(migration, "Down");
            InvokeModelBuilderMethod(migration, "BuildTargetModel");
        }
    }

    [Fact]
    public void SampleHostModelSnapshotsCanBuildModels()
    {
        var assemblies = new[]
        {
            Assembly.Load("KnOwl.ControlPlaneHost.Sample"),
            Assembly.Load("KnOwl.RuntimeHost.Sample")
        };

        var snapshots = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && typeof(ModelSnapshot).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(snapshots);

        foreach (var snapshotType in snapshots)
        {
            var snapshot = (ModelSnapshot)Activator.CreateInstance(snapshotType)!;

            InvokeModelBuilderMethod(snapshot, "BuildModel");
        }
    }

    private static void InvokeMigrationBuilderMethod(Migration migration, string methodName)
    {
        var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        method.Invoke(migration, [builder]);

        Assert.NotNull(builder.Operations);
    }

    private static void InvokeModelBuilderMethod(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var modelBuilder = new ModelBuilder(new ConventionSet());
        method.Invoke(target, [modelBuilder]);

        Assert.NotNull(modelBuilder.Model);
    }
}
