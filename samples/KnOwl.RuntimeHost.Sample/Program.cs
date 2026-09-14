using KnOwl.Runtime.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKnOwlRuntime(
    builder.Configuration,
    options => options.MigrationsAssembly = typeof(Program).Assembly.GetName().Name);

var app = builder.Build();

app.MapKnOwlRuntime();

app.Run();

/// <summary>
/// Runtime host program marker used by integration tests.
/// </summary>
public partial class Program;
