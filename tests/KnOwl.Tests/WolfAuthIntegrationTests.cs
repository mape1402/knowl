using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using KnOwl.WolfAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnOwl.Tests;

public sealed class WolfAuthIntegrationTests
{
    [Fact]
    public async Task LoginGateRedirectsAnonymousBrowserRequestsToLoginShell()
    {
        await using var fixture = await WolfAuthFixture.Start();

        using var response = await fixture.Client.GetAsync("/secure?x=1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/auth/login?returnUrl=%2Fsecure%3Fx%3D1", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task LoginShellRendersConfiguredLoginButton()
    {
        await using var fixture = await WolfAuthFixture.Start(options =>
        {
            options.ApplicationName = "Sample KnOwl";
            options.Subtitle = "Authentication required.";
            options.LoginButtonText = "Login";
        });

        var html = await fixture.Client.GetStringAsync("/auth/login?returnUrl=/secure");

        Assert.Contains("Sample KnOwl", html);
        Assert.Contains("Authentication required.", html);
        Assert.Contains(">Login</a>", html);
        Assert.Contains("/auth/login/challenge?returnUrl=%2Fsecure", html);
    }

    [Fact]
    public async Task ChallengeEndpointUsesConfiguredAuthenticationScheme()
    {
        await using var fixture = await WolfAuthFixture.Start(options =>
            options.ChallengeSchemes.Add(TestAuthenticationDefaults.Scheme));

        using var response = await fixture.Client.GetAsync("/auth/login/challenge?returnUrl=/secure");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/external-login?returnUrl=%2Fsecure", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AuthenticatedRequestsReachTheApplication()
    {
        await using var fixture = await WolfAuthFixture.Start(
            configureOptions: null,
            authenticated: true);

        var content = await fixture.Client.GetStringAsync("/secure");

        Assert.Equal("ok", content);
    }

    [Fact]
    public async Task IntegrationIsNoOpWhenUseWolfAuthWasNotRegistered()
    {
        await using var fixture = await WolfAuthFixture.Start(
            configureOptions: null,
            authenticated: false,
            registerKnOwlWolfAuth: false);

        var content = await fixture.Client.GetStringAsync("/secure");

        Assert.Equal("ok", content);
    }

    private sealed class WolfAuthFixture(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public static async Task<WolfAuthFixture> Start(
            Action<KnOwlWolfAuthOptions>? configureOptions = null,
            bool authenticated = false,
            bool registerKnOwlWolfAuth = true)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddAuthentication(TestAuthenticationDefaults.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationDefaults.Scheme,
                    options => { });
            builder.Services.AddAuthorization();

            builder.Services.Configure<TestAuthenticationState>(options => options.IsAuthenticated = authenticated);

            if (registerKnOwlWolfAuth)
            {
                builder.Services.UseWolfAuth(configureOptions);
            }

            var app = builder.Build();
            app.UseAuthentication();
            app.UseKnOwlWolfAuthLoginGate();
            app.UseAuthorization();
            app.MapKnOwlWolfAuthEndpoints();
            app.MapGet("/secure", () => "ok");

            await app.StartAsync();
            return new WolfAuthFixture(app, app.GetTestClient());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private static class TestAuthenticationDefaults
    {
        public const string Scheme = "test";
    }

    private sealed class TestAuthenticationState
    {
        public bool IsAuthenticated { get; set; }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<TestAuthenticationState> state)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!state.Value.IsAuthenticated)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-1"), new Claim(ClaimTypes.Name, "Test User")],
                Scheme.Name);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var returnUrl = properties.RedirectUri ?? "/";
            Response.Redirect($"/external-login?returnUrl={WebUtility.UrlEncode(returnUrl)}");
            return Task.CompletedTask;
        }
    }
}
