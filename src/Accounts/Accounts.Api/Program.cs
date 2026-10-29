using Accounts.Api;
using Accounts.Application;
using Accounts.Infrastructure;
using Accounts.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsDefaults(typeof(Program).Assembly);
builder.Services.AddAccountsApplication();
builder.Services.AddAccountsInfrastructure(builder.Configuration);
builder.Services.AddAccountsApi();

var app = builder.Build();

app.UseHomeAppsDefaults();

// After authentication and authorization, so the limit is counted per signed-in user (also a ticket holder); the
// endpoints themselves run last, after every middleware added here.
app.UseRateLimiter();

await app.MigrateDatabaseOnStartupAsync<AccountsDbContext>();

await app.RunAsync();
