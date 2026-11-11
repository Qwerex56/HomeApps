using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Mcp;
using Households.Api;
using Households.Application;
using Households.Infrastructure;
using Households.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsDefaults(typeof(Program).Assembly);
builder.AddHomeAppsMcp("homeapps-households", typeof(Program).Assembly);
builder.Services.AddHouseholdsApi(builder.Configuration);
builder.Services.AddHouseholdsApplication();
builder.Services.AddHouseholdsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseHomeAppsDefaults();

// Inside the Docker network only; JARVIS reads through it (tools only read, the rest refuses an assistant).
app.MapHomeAppsMcp();

// After authentication and authorization, so the limits are counted per signed-in user.
app.UseRateLimiter();

await app.MigrateDatabaseOnStartupAsync<HouseholdsDbContext>();

await app.RunAsync();
