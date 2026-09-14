using KnOwl.ControlPlane.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKnOwlControlPlane(
    builder.Configuration,
    options => options.MigrationsAssembly = typeof(Program).Assembly.GetName().Name);

var app = builder.Build();

app.MapKnOwlControlPlane();

app.Run();

/// <summary>
/// Control Plane host program marker used by integration tests.
/// </summary>
public partial class Program;
