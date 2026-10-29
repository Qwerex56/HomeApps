using HomeApps.Plugins;
using HomeApps.Plugins.Hello;
using HomeApps.Plugins.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsPlugin(HelloPlugin.Manifest, typeof(Program).Assembly);
builder.Services.AddHomeAppsPluginApplication(HelloPlugin.Manifest, typeof(Program).Assembly);
builder.Services.AddHomeAppsPluginInfrastructure<HelloDbContext>(builder.Configuration, HelloPlugin.Manifest, typeof(Program).Assembly);

var app = builder.Build();

app.UseHomeAppsPlugin();

await app.RunAsync();

public partial class Program;
