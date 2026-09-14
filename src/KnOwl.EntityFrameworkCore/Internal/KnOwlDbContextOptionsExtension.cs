using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnOwl.EntityFrameworkCore.Internal;

internal sealed class KnOwlDbContextOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo _info;

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<IModelCustomizer, KnOwlModelCustomizer>());
    }

    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        {
        }

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "using KnOwl ";

        public override int GetServiceProviderHashCode()
            => typeof(KnOwlDbContextOptionsExtension).GetHashCode();

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["KnOwl"] = "1";

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;
    }
}
