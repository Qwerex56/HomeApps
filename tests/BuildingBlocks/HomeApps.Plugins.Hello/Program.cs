using HomeApps.Plugins;
using HomeApps.Plugins.Hello;
using HomeApps.Plugins.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsPlugin(HelloPlugin.Manifest, typeof(Program).Assembly);
builder.Services.AddHomeAppsPluginApplication(HelloPlugin.Manifest, typeof(Program).Assembly);
builder.Services.AddHomeAppsPluginInfrastructure<HelloDbContext>(builder.Configuration, HelloPlugin.Manifest, typeof(Program).Assembly);

var app = builder.Build();

app.UseHomeAppsPlugin();

// Stands in for a realtime hub (SignalR in a real plugin): the token comes as ?access_token=.
app.MapGet("/live/households/{householdId:guid}/me", async (Guid householdId, IHouseholdAccess access, CancellationToken ct) =>
        await access.GetMembershipAsync(householdId, ct) is { } membership ? Results.Ok(membership) : Results.NotFound())
    .RequireAuthorization();

await app.RunAsync();

public partial class Program;
