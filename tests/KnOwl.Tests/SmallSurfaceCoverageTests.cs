using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ControlPlaneErrorModel = KnOwl.ControlPlane.WebUI.Pages.ErrorModel;
using RuntimeErrorModel = KnOwl.Runtime.WebUI.Pages.ErrorModel;

namespace KnOwl.Tests;

public sealed class SmallSurfaceCoverageTests
{
    [Fact]
    public void ErrorPagesExposeRequestIdentifiers()
    {
        var controlPlane = new ControlPlaneErrorModel();
        AttachContext(controlPlane, "control-plane-request");

        Assert.False(controlPlane.ShowRequestId);
        controlPlane.OnGet();

        var runtime = new RuntimeErrorModel();
        AttachContext(runtime, "runtime-request");
        runtime.OnGet();

        Assert.Equal("control-plane-request", controlPlane.RequestId);
        Assert.True(controlPlane.ShowRequestId);
        Assert.Equal("runtime-request", runtime.RequestId);
    }

    [Fact]
    public void DocumentationApplicationRegistrationAddsDefaultServicesAndOptions()
    {
        var services = new ServiceCollection();

        services.AddKnOwlDocumentationApplication(options => options.MaxPackageBytes = 123);

        using var provider = services.BuildServiceProvider();
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IDocumentationInteractionService) &&
            descriptor.ImplementationType == typeof(DocumentationInteractionService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IDocumentationPdfRenderer) &&
            descriptor.ImplementationType == typeof(SimpleDocumentationPdfRenderer) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        Assert.Equal(123, provider.GetRequiredService<IOptions<DocumentationOptions>>().Value.MaxPackageBytes);
    }

    private static void AttachContext(PageModel model, string traceIdentifier)
    {
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = traceIdentifier
            }
        };
    }
}
