using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using Households.Api;
using Households.Application;
using Households.Infrastructure;
using Households.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsDefaults(typeof(Program).Assembly);
builder.Services.AddHouseholdsApi(builder.Configuration);
builder.Services.AddHouseholdsApplication();
builder.Services.AddHouseholdsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseHomeAppsDefaults();

// After authentication and authorization, so the limits are counted per signed-in user.
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync<HouseholdsDbContext>();
}

await app.RunAsync();
