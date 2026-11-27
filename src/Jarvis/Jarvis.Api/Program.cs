using System.Text.Json.Serialization;
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
builder.Services.AddSingleton<Jarvis.Api.Voice.TurnLeases>();

// Statuses and roles as names, like the other services the web app reads.
builder.Services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// The voice listeners' sockets (/jarvis/voice); pings keep Cloudflare and Traefik from closing a quiet one.
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });
app.UseHomeAppsDefaults();
app.MapJarvisGateway();

await app.MigrateDatabaseOnStartupAsync<JarvisDbContext>();

await app.RunAsync();

public partial class Program;
