using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using ServiceName.Application;
using ServiceName.Infrastructure;
using ServiceName.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsDefaults(typeof(Program).Assembly);
builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync<ServiceNameDbContext>();
}

app.UseHomeAppsDefaults();

await app.RunAsync();

public partial class Program;
