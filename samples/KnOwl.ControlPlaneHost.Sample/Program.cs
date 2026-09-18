using KnOwl.ControlPlane.Bootstrap;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var migrationsAssembly = typeof(Program).Assembly.GetName().Name!;
var connectionString = builder.Configuration.GetConnectionString("KnOwlDb")
    ?? throw new InvalidOperationException("ConnectionStrings:KnOwlDb is required.");

builder.Services.AddKnOwlControlPlane(builder.Configuration, options =>
{
    options.MigrationsAssembly = migrationsAssembly;
    options.ConfigureStorage = db => db.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly(migrationsAssembly));
});

var app = builder.Build();

app.MapKnOwlControlPlane();

app.Run();

/// <summary>
/// Control Plane host program marker used by integration tests.
/// </summary>
public partial class Program;
