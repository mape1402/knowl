using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace KnOwl.EntityFrameworkCore.Internal;

internal sealed class KnOwlModelCustomizer : ModelCustomizer
{
    public KnOwlModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        modelBuilder.ApplyKnOwl();
    }
}
