using KnOwl.WolfAuth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using KnOwl.ControlPlane.Bootstrap;
using KnOwl.ControlPlaneHost.Sample.Design;
using KnOwl.ControlPlaneHost.Sample.Documentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using WolfAuth.AspNetCore;
using WolfAuth.Microsoft.EntraId;

var builder = WebApplication.CreateBuilder(args);
var migrationsAssembly = typeof(Program).Assembly.GetName().Name!;
var connectionString = builder.Configuration.GetConnectionString("KnOwlDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:KnOwlDb is required.");
}

var entraId = EntraIdSettings.FromConfiguration(builder.Configuration);
if (entraId.IsConfigured)
{
    builder.Services.AddSingleton<IWolfAuthEntraIdProvisioningMapper, WolfAuthEntraIdProvisioningMapper>();
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.Cookie.Name = "KnOwl.Sample.Auth";
            options.LoginPath = "/auth/login";
            options.LogoutPath = "/auth/logout";
        })
        .AddOpenIdConnect(options =>
        {
            options.Authority = $"https://login.microsoftonline.com/{entraId.TenantId}/v2.0";
            options.ClientId = entraId.ClientId;
            options.ClientSecret = entraId.ClientSecret;
            options.CallbackPath = entraId.CallbackPath;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = true;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.RoleClaimType = "roles";
        });

    builder.Services.AddWolfAuth(wolf =>
    {
        wolf.ClaimsMapping.DefaultProvider = "microsoft-entra-id";
    });
}

var knowlServices = builder.Services.AddKnOwlControlPlane(builder.Configuration, options =>
{
    options.MigrationsAssembly = migrationsAssembly;
    options.Theme.Title = "Sample KnOwl";
    options.Theme.Mode = KnOwl.ControlPlane.WebUI.KnOwlThemeMode.Dark;
    options.Theme.IconImageUrl = "/_content/KnOwl.ControlPlane.WebUI/img/knowl-icon.png";
    options.Theme.Light.PrimaryColor = "#2563eb";
    options.Theme.Light.PrimaryHoverColor = "#1d4ed8";
    options.Theme.Light.SidebarBackgroundColor = "#0f2f5f";
    options.Theme.Light.SidebarBrandBackgroundColor = "#0b2347";
    options.Theme.Light.SidebarTextColor = "#ffffff";
    options.Theme.Light.SidebarMutedTextColor = "#bfdbfe";
    options.Theme.Dark.PrimaryColor = "#60a5fa";
    options.Theme.Dark.PrimaryHoverColor = "#93c5fd";
    options.Theme.Dark.SidebarBackgroundColor = "#0f172a";
    options.Theme.Dark.SidebarBrandBackgroundColor = "#0b1220";
    options.Theme.Dark.SidebarTextColor = "#f8fafc";
    options.Theme.Dark.SidebarMutedTextColor = "#bfdbfe";
    options.ConfigureStorage = db => db.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly(migrationsAssembly));
});

if (entraId.IsConfigured)
{
    knowlServices.UseWolfAuth(options =>
    {
        options.ApplicationName = "Sample KnOwl";
        options.Subtitle = "Sign in with Microsoft Entra ID.";
        options.LoginButtonText = "Login";
        options.ChallengeSchemes.Add(OpenIdConnectDefaults.AuthenticationScheme);
        options.SignOutSchemes.Add(CookieAuthenticationDefaults.AuthenticationScheme);
        options.SignOutSchemes.Add(OpenIdConnectDefaults.AuthenticationScheme);
    });
}

var app = builder.Build();

await SampleDesignSeeder.Initialize(app);
await SampleDocumentationSeeder.Initialize(app);

app.MapKnOwlControlPlane();

app.Run();

/// <summary>
/// Control Plane host program marker used by integration tests.
/// </summary>
public partial class Program;

internal sealed record EntraIdSettings(
    string? TenantId,
    string? ClientId,
    string? ClientSecret,
    string CallbackPath)
{
    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(TenantId) &&
           !string.IsNullOrWhiteSpace(ClientId) &&
           !string.IsNullOrWhiteSpace(ClientSecret);

    public static EntraIdSettings FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("EntraId");
        return new EntraIdSettings(
            section["TenantId"],
            section["ClientId"],
            section["ClientSecret"],
            string.IsNullOrWhiteSpace(section["CallbackPath"])
                ? "/signin-oidc"
                : section["CallbackPath"]!);
    }
}
