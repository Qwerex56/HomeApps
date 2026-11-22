using Devices.Api;
using Devices.Application;
using Devices.Infrastructure;
using Devices.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsDefaults(typeof(Program).Assembly);
builder.Services.AddDevicesApplication();
builder.Services.AddDevicesInfrastructure(builder.Configuration);
builder.AddDevicesApi();

var app = builder.Build();

// The devices' control sockets (/devices/link); pings keep Cloudflare and Traefik from closing a quiet one.
app.UseForwardedHeaders();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });
app.UseHomeAppsDefaults();
app.UseRateLimiter();

// Inside the Docker network only; JARVIS's gateway lists and calls the devices' actions through it.
app.MapHomeAppsMcp();

await app.MigrateDatabaseOnStartupAsync<DevicesDbContext>();

await app.RunAsync();

public partial class Program;
