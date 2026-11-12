using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using Jarvis.Api.Gateway;
using Jarvis.Application;
using Jarvis.Infrastructure;
using Jarvis.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsDefaults(typeof(Program).Assembly);
builder.Services.AddJarvisApplication();
builder.Services.AddJarvisInfrastructure(builder.Configuration);
builder.AddJarvisGateway();

var app = builder.Build();

app.UseHomeAppsDefaults();
app.MapJarvisGateway();

await app.MigrateDatabaseOnStartupAsync<JarvisDbContext>();

await app.RunAsync();

public partial class Program;
