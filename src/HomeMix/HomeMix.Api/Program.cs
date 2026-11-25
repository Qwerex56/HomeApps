using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Mcp;
using HomeMix.Api;
using HomeMix.Api.Realtime;
using HomeMix.Application;
using HomeMix.Infrastructure;
using HomeMix.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

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

// The web app's realtime connection (docs/homemix.md, "RealTime").
app.MapHomeMixRealtime();

// After authentication, before the endpoints run: notify.push is limited per installation.
app.UseRateLimiter();

// A missing HOMEMIX_CLIENT_SECRET stops the start here, before any migration (ValidateOnStart alone runs once the host starts).
_ = app.Services.GetRequiredService<IOptions<HomeMixOptions>>().Value;

await app.MigrateDatabaseOnStartupAsync<HomeMixDbContext>();

await app.RunAsync();
