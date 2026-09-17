using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using KnOwl.ControlPlane.Api;
using KnOwl.ControlPlane.Api.Contracts;
using KnOwl.Security;
using KnOwl.Security.Authorization;
using KnOwl.Security.Storage;
using KnOwl.Security.Subjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnOwl.Tests;

public sealed class SecurityAuthorizationTests
{
    private const string TestScheme = "Test";
    private const string Provider = "test";

    [Fact]
    public async Task BootstrapAdminReceivesAllPermissionsAndIsSynchronized()
    {
        var store = new InMemoryKnOwlSecurityStore();
        var service = CreateAuthorizationService(store, options =>
        {
            options.BootstrapAdmins.Add(new KnOwlBootstrapSubject { Provider = Provider, SubjectId = "root" });
        });

        var allowed = await service.HasPermission(
            CreatePrincipal("root"),
            KnOwlPermissions.SecurityManage,
            KnOwlAuthorizationScope.Global);

        Assert.True(allowed);
        Assert.NotNull(await store.GetSubject(Provider, "root"));
        Assert.Contains(await store.GetRoleAssignments(Provider, "root"), x => x.Role == KnOwlRoles.Admin);
    }

    [Fact]
    public async Task UnknownSubjectIsDeniedWhenKnownSubjectsAreRequired()
    {
        var service = CreateAuthorizationService(new InMemoryKnOwlSecurityStore(), options => options.RequireKnownSubject = true);

        var allowed = await service.HasPermission(
            CreatePrincipal("unknown"),
            KnOwlPermissions.CommandsWrite,
            KnOwlAuthorizationScope.Global);

        Assert.False(allowed);
    }

    [Fact]
    public async Task DirectRoleGrantsRolePermissions()
    {
        var store = new InMemoryKnOwlSecurityStore();
        await store.UpsertSubject(new KnOwlSubject { Provider = Provider, SubjectId = "designer" });
        await store.AssignRole(new KnOwlRoleAssignment
        {
            Provider = Provider,
            SubjectId = "designer",
            Role = KnOwlRoles.Designer,
            ScopeType = KnOwlAuthorizationScopeTypes.Global,
            ScopeId = "*"
        });
        var service = CreateAuthorizationService(store, options => options.RequireKnownSubject = true);

        var allowed = await service.HasPermission(
            CreatePrincipal("designer"),
            KnOwlPermissions.CommandsWrite,
            KnOwlAuthorizationScope.Global);

        Assert.True(allowed);
    }

    [Fact]
    public async Task ExternalGroupRoleGrantsRolePermissions()
    {
        var store = new InMemoryKnOwlSecurityStore();
        await store.UpsertSubject(new KnOwlSubject { Provider = Provider, SubjectId = "release-user" });
        await store.AssignExternalGroupRole(new KnOwlExternalGroupRoleAssignment
        {
            Provider = Provider,
            ExternalGroupId = "release-managers",
            Role = KnOwlRoles.ReleaseManager,
            ScopeType = KnOwlAuthorizationScopeTypes.Global,
            ScopeId = "*"
        });
        var service = CreateAuthorizationService(store, options => options.RequireKnownSubject = true);

        var allowed = await service.HasPermission(
            CreatePrincipal("release-user", "release-managers"),
            KnOwlPermissions.ReleasesExecute,
            KnOwlAuthorizationScope.Global);

        Assert.True(allowed);
    }

    [Fact]
    public async Task DisabledSubjectIsDeniedEvenWhenAssignedRole()
    {
        var store = new InMemoryKnOwlSecurityStore();
        await store.UpsertSubject(new KnOwlSubject { Provider = Provider, SubjectId = "disabled", IsEnabled = false });
        await store.AssignRole(new KnOwlRoleAssignment
        {
            Provider = Provider,
            SubjectId = "disabled",
            Role = KnOwlRoles.Admin,
            ScopeType = KnOwlAuthorizationScopeTypes.Global,
            ScopeId = "*"
        });
        var service = CreateAuthorizationService(store);

        var allowed = await service.HasPermission(
            CreatePrincipal("disabled"),
            KnOwlPermissions.SecurityManage,
            KnOwlAuthorizationScope.Global);

        Assert.False(allowed);
    }

    [Fact]
    public async Task ControlPlaneSecurityEndpointRequiresAuthentication()
    {
        await using var app = await CreateSecurityApi(new InMemoryKnOwlSecurityStore());
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/api/v1/control-plane/security/subjects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ControlPlaneSecurityEndpointDeniesKnownSubjectWithoutSecurityRole()
    {
        var store = new InMemoryKnOwlSecurityStore();
        await store.UpsertSubject(new KnOwlSubject { Provider = Provider, SubjectId = "reader" });
        await store.AssignRole(new KnOwlRoleAssignment
        {
            Provider = Provider,
            SubjectId = "reader",
            Role = KnOwlRoles.Reader,
            ScopeType = KnOwlAuthorizationScopeTypes.Global,
            ScopeId = "*"
        });
        await using var app = await CreateSecurityApi(store);
        using var http = CreateClient(app, "reader");

        using var response = await http.GetAsync("/api/v1/control-plane/security/subjects");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ControlPlaneSecurityEndpointAllowsSecurityAdmin()
    {
        var store = new InMemoryKnOwlSecurityStore();
        await store.UpsertSubject(new KnOwlSubject { Provider = Provider, SubjectId = "security-admin" });
        await store.AssignRole(new KnOwlRoleAssignment
        {
            Provider = Provider,
            SubjectId = "security-admin",
            Role = KnOwlRoles.SecurityAdmin,
            ScopeType = KnOwlAuthorizationScopeTypes.Global,
            ScopeId = "*"
        });
        await using var app = await CreateSecurityApi(store);
        using var http = CreateClient(app, "security-admin");

        using var response = await http.PutAsJsonAsync("/api/v1/control-plane/security/subjects", new UpsertSecuritySubjectRequest(
            Provider,
            "designer",
            "Designer User",
            "designer@example.test"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(await store.GetSubject(Provider, "designer"));
    }

    private static IKnOwlAuthorizationService CreateAuthorizationService(
        InMemoryKnOwlSecurityStore store,
        Action<KnOwlSecurityOptions>? configure = null)
    {
        var options = new KnOwlSecurityOptions();
        options.Subject.Provider = Provider;
        configure?.Invoke(options);
        return new KnOwlAuthorizationService(new ClaimsKnOwlSubjectResolver(Options.Create(options)), store, Options.Create(options));
    }

    private static async Task<WebApplication> CreateSecurityApi(InMemoryKnOwlSecurityStore store)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services
            .AddAuthentication(TestScheme)
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(TestScheme, _ => { });
        builder.Services.AddKnOwlSecurity(options =>
        {
            options.RequireKnownSubject = true;
            options.Subject.Provider = Provider;
        });
        builder.Services.AddSingleton<IKnOwlSecurityStore>(store);

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapKnOwlControlPlaneApi(new KnOwlApiAuthorizationOptions
        {
            SecurityManagePolicy = KnOwlAuthorizationPolicies.SecurityManage
        });
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app, string? subjectId = null)
    {
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The test server did not expose an address.");
        var http = new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
        if (!string.IsNullOrWhiteSpace(subjectId))
        {
            http.DefaultRequestHeaders.Add("X-Test-Subject", subjectId);
        }

        return http;
    }

    private static ClaimsPrincipal CreatePrincipal(string subjectId, params string[] groups)
    {
        var claims = new List<Claim>
        {
            new("oid", subjectId),
            new("name", subjectId)
        };
        claims.AddRange(groups.Select(group => new Claim("groups", group)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, TestScheme));
    }

    private sealed class HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var subjectId = Request.Headers["X-Test-Subject"].ToString();
            if (string.IsNullOrWhiteSpace(subjectId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new("oid", subjectId),
                new("name", subjectId)
            };
            claims.AddRange(Request.Headers["X-Test-Groups"].ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(group => new Claim("groups", group)));

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
