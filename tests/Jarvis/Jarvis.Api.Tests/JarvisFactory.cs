using System.Collections.Concurrent;
using System.Net.Http.Headers;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Testing;
using Jarvis.Application.Gateway;
using Jarvis.Infrastructure.Gateway;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Voice;
using Jarvis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Api.Tests;

/// <summary>
/// The gateway in memory with three sources in memory too: Households, the Todo plugin (found through its manifest)
/// and HomeMix. Accounts is configured but never answers, like a service that is down. JARVIS's brain is
/// <see cref="FakeBrain"/>: a test says which tools it calls, through the gateway like Claude Code would.
/// </summary>
public sealed class JarvisFactory : HomeAppsApiFactory<Program, JarvisDbContext>, IAsyncLifetime
{
    public const string Resource = "https://app.test/jarvis/mcp";

    private readonly ConcurrentDictionary<string, HttpMessageHandler> _hosts = new();
    private readonly List<FakeSource> _sources = [];

    internal FakeDelegation Delegation { get; } = new();

    internal FakeSpeakers Speakers { get; } = new();

    internal FakeCeilings Ceilings { get; } = new();

    internal FakeHouseholdDevices Devices { get; } = new();

    internal FakeBrain Brain { get; } = new();

    /// <summary>The "devices" source (kind Devices): no tools until a test sets some.</summary>
    internal FakeToolSet DevicesSource { get; } = new();

    /// <summary>A second, ordinary (core) source named "extra": no tools until a test sets some.</summary>
    internal FakeToolSet ExtraSource { get; } = new();

    internal FakeEars Ears { get; } = new();

    /// <summary>What JARVIS published, e.g. his answers to a channel.</summary>
    public ConcurrentQueue<object> Published { get; } = new();

    public new async Task InitializeAsync()
    {
        await AddAsync("households.test", await FakeSource.StartAsync("/mcp", m => m.WithTools<HouseholdsTools>()));
        await AddAsync("todo.test", await FakeSource.StartAsync("/plugins/todo/mcp", m => m.WithTools<TodoTools>(), pluginId: "todo"));
        await AddAsync("homemix.test", await FakeSource.StartAsync("/mcp", m => m.WithTools<HomeMixTools>().WithResources<HomeMixResources>()));
        await AddAsync("devices.test", await FakeSource.StartAsync("/mcp", m => Serve(m, DevicesSource)));
        await AddAsync("extra.test", await FakeSource.StartAsync("/mcp", m => Serve(m, ExtraSource)));
        await base.InitializeAsync();
        Brain.Gateway = token => CreateClient(token);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        foreach (var source in _sources)
        {
            await source.DisposeAsync();
        }
    }

    public HttpClient CreateClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Jarvis:Resource", Resource);
        builder.UseSetting("Jarvis:ClientSecret", "test-secret");
        builder.UseSetting("Jarvis:Plugins", "todo");
        builder.UseSetting("Jarvis:PluginUrl", "http://{id}.test");
        builder.UseSetting("Jarvis:Sources:0:Name", "households");
        builder.UseSetting("Jarvis:Sources:0:Url", "http://households.test/mcp");
        builder.UseSetting("Jarvis:Sources:1:Name", "accounts");
        builder.UseSetting("Jarvis:Sources:1:Url", "http://accounts.test/mcp");
        builder.UseSetting("Jarvis:Sources:2:Name", "homemix");
        builder.UseSetting("Jarvis:Sources:2:Url", "http://homemix.test/mcp");
        builder.UseSetting("Jarvis:Sources:2:Prefix", string.Empty);

        builder.UseSetting("Jarvis:Sources:3:Name", "devices");
        builder.UseSetting("Jarvis:Sources:3:Url", "http://devices.test/mcp");
        builder.UseSetting("Jarvis:Sources:3:Prefix", "device");
        builder.UseSetting("Jarvis:Sources:4:Name", "extra");
        builder.UseSetting("Jarvis:Sources:4:Url", "http://extra.test/mcp");

        builder.UseSetting("Jarvis:DailyBudgetUsd", "1");
        builder.UseSetting("Jarvis:Voice:Enabled", "true");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDelegation>(Delegation);
        services.AddSingleton<IAssistantBrain>(Brain);
        services.AddSingleton<ISpeechToText>(Ears);
        services.AddSingleton<ISpeakers>(Speakers);
        services.AddSingleton<IHouseholdCeilings>(Ceilings);
        services.AddSingleton<IHouseholdDevices>(Devices);
        services.AddSingleton<ITextToSpeech, FakeVoice>();
        services.AddScoped<IEventBus>(sp => new RecordingEventBus(sp.GetRequiredService<MassTransit.IPublishEndpoint>(), Published));
        services.AddHttpClient(SourceConnector.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => new HostRouter(_hosts));
        services.AddHttpClient(nameof(IToolSources)).ConfigurePrimaryHttpMessageHandler(() => new HostRouter(_hosts));
    }

    private static void Serve(IMcpServerBuilder server, FakeToolSet set) => server
        .WithListToolsHandler((_, _) => ValueTask.FromResult(set.List()))
        .WithCallToolHandler((context, _) => ValueTask.FromResult(set.Call(context.Params!.Name)));

    private Task AddAsync(string host, FakeSource source)
    {
        _sources.Add(source);
        _hosts[host] = source.Handler;
        return Task.CompletedTask;
    }
}
