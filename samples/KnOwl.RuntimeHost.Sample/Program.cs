using KnOwl.Runtime.Bootstrap;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var migrationsAssembly = typeof(Program).Assembly.GetName().Name!;
var connectionString = builder.Configuration.GetConnectionString("KnOwlRuntimeDb")
    ?? builder.Configuration.GetConnectionString("KnOwlDb")
    ?? throw new InvalidOperationException("ConnectionStrings:KnOwlRuntimeDb or ConnectionStrings:KnOwlDb is required.");

builder.Services.AddKnOwlRuntime(builder.Configuration, options =>
{
    options.MigrationsAssembly = migrationsAssembly;
    options.ConfigureStorage = db => db.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly(migrationsAssembly));
});

var app = builder.Build();

app.MapKnOwlRuntime();

app.Run();

/// <summary>
/// Runtime host program marker used by integration tests.
/// </summary>
public partial class Program;
