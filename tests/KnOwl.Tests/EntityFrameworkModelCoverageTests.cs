using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using KnOwl.Documentation.Storage.EntityFramework.Data;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using KnOwl.Security.Storage.EntityFramework.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Tests;

public sealed class EntityFrameworkModelCoverageTests
{
    [Fact]
    public void ControlPlaneContextBuildsTheCompleteRelationalModel()
    {
        using var context = new KnOwlDbContext(CreateOptions<KnOwlDbContext>());

        Assert.NotEmpty(context.Events.EntityType.GetProperties());
        Assert.NotEmpty(context.EventVersions.EntityType.GetProperties());
        Assert.NotEmpty(context.Commands.EntityType.GetProperties());
        Assert.NotEmpty(context.CommandVersions.EntityType.GetProperties());
        Assert.NotEmpty(context.SchemaTypes.EntityType.GetProperties());
        Assert.NotEmpty(context.SchemaTypeVersions.EntityType.GetProperties());
        Assert.NotEmpty(context.ContractFieldMetadataDefinitions.EntityType.GetProperties());
        Assert.NotEmpty(context.ContractFieldMetadataVersions.EntityType.GetProperties());
        Assert.NotEmpty(context.ContractArtifacts.EntityType.GetProperties());
        Assert.NotEmpty(context.ContractReleases.EntityType.GetProperties());
        Assert.NotEmpty(context.ContractReleaseItems.EntityType.GetProperties());
        Assert.NotEmpty(context.RuntimeEnvironments.EntityType.GetProperties());
        Assert.NotEmpty(context.RuntimeNodes.EntityType.GetProperties());
        Assert.NotEmpty(context.ContractReleaseTargets.EntityType.GetProperties());
        Assert.NotEmpty(context.ContractReleaseAttempts.EntityType.GetProperties());
        Assert.NotEmpty(context.RuntimeContractArtifacts.EntityType.GetProperties());
        Assert.True(context.Model.GetEntityTypes().Count() >= 16);
    }

    [Fact]
    public void RuntimeContextBuildsTheCompleteRelationalModel()
    {
        using var context = new KnOwlRuntimeDbContext(CreateOptions<KnOwlRuntimeDbContext>());

        Assert.NotEmpty(context.RuntimeContractArtifacts.EntityType.GetProperties());
        Assert.NotEmpty(context.RuntimeDesignNodes.EntityType.GetProperties());
        Assert.True(context.Model.GetEntityTypes().Count() >= 2);
    }

    [Fact]
    public void DocumentationContextBuildsTheCompleteRelationalModel()
    {
        using var context = new KnOwlDocumentationDbContext(CreateOptions<KnOwlDocumentationDbContext>());

        Assert.NotEmpty(context.Spaces.EntityType.GetProperties());
        Assert.NotEmpty(context.Topics.EntityType.GetProperties());
        Assert.NotEmpty(context.Pages.EntityType.GetProperties());
        Assert.NotEmpty(context.PageVersions.EntityType.GetProperties());
        Assert.NotEmpty(context.Assets.EntityType.GetProperties());
        Assert.NotEmpty(context.ContentBlobs.EntityType.GetProperties());
        Assert.True(context.Model.GetEntityTypes().Count() >= 6);
    }

    [Fact]
    public void SecurityContextBuildsTheCompleteRelationalModel()
    {
        using var context = new KnOwlSecurityDbContext(CreateOptions<KnOwlSecurityDbContext>());

        Assert.NotEmpty(context.Subjects.EntityType.GetProperties());
        Assert.NotEmpty(context.RoleAssignments.EntityType.GetProperties());
        Assert.NotEmpty(context.PermissionAssignments.EntityType.GetProperties());
        Assert.NotEmpty(context.ExternalGroupRoleAssignments.EntityType.GetProperties());
        Assert.True(context.Model.GetEntityTypes().Count() >= 4);
    }

    private static DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=KnOwlCoverage;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
    }
}
