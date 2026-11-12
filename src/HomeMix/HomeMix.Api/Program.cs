using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Mcp;
using HomeMix.Api;
using HomeMix.Application;
using HomeMix.Infrastructure;
using HomeMix.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsDefaults(typeof(Program).Assembly);
// The add-ons' tools for JARVIS (A6): the JARVIS gateway lists this source per person (jarvis-v1.md 10a).
builder.AddHomeAppsMcp("homeapps-homemix", typeof(Program).Assembly);
builder.Services.AddHomeMixApi();
builder.Services.AddHomeMixApplication();
builder.Services.AddHomeMixInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseHomeAppsDefaults();

// Inside the Docker network only, like Households' (Traefik routes /homemix/api and /homemix/run, not /mcp).
app.MapHomeAppsMcp();

// After authentication, before the endpoints run: notify.push is limited per installation.
app.UseRateLimiter();

await app.MigrateDatabaseOnStartupAsync<HomeMixDbContext>();

await app.RunAsync();
