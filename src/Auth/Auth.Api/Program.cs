using Auth.Api;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsDefaults(typeof(Program).Assembly);
builder.Services.AddAuthApplication(builder.Configuration);
builder.Services.AddAuthInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddAuthApi(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseHomeAppsDefaults();

if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync<AuthDbContext>();
}

await app.RunAsync();
