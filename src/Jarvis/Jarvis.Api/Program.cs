using HomeApps.BuildingBlocks.Api;
using Jarvis.Api.Gateway;
using Jarvis.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsDefaults(typeof(Program).Assembly);
builder.Services.AddJarvisInfrastructure();
builder.AddJarvisGateway();

var app = builder.Build();

app.UseHomeAppsDefaults();
app.MapJarvisGateway();

await app.RunAsync();

public partial class Program;
