using System.Collections.Concurrent;
using System.Net.Http.Headers;
using HomeApps.BuildingBlocks.Testing;
using Jarvis.Application.Gateway;
using Jarvis.Infrastructure.Gateway;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Api.Tests;

/// <summary>
/// The gateway in memory with three sources in memory too: Households, the Todo plugin (found through its manifest)
/// and HomeMix. Accounts is configured but never answers, like a service that is down.
/// </summary>
public sealed class JarvisFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Resource = "https://app.test/jarvis/mcp";

    private readonly ConcurrentDictionary<string, HttpMessageHandler> _hosts = new();
    private readonly List<FakeSource> _sources = [];

    internal FakeDelegation Delegation { get; } = new();

    public async Task InitializeAsync()
    {
        await AddAsync("households.test", await FakeSource.StartAsync("/mcp", m => m.WithTools<HouseholdsTools>()));
        await AddAsync("todo.test", await FakeSource.StartAsync("/plugins/todo/mcp", m => m.WithTools<TodoTools>(), pluginId: "todo"));
        await AddAsync("homemix.test", await FakeSource.StartAsync("/mcp", m => m.WithTools<HomeMixTools>().WithResources<HomeMixResources>()));
        _ = Services;
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
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Authority", TestJwt.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwt.Audience);
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

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IDelegation>(Delegation);
            services.AddHttpClient(SourceConnector.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => new HostRouter(_hosts));
            services.AddHttpClient(nameof(IToolSources)).ConfigurePrimaryHttpMessageHandler(() => new HostRouter(_hosts));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.MetadataAddress = null!;
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration { Issuer = TestJwt.Issuer };
                options.Configuration.SigningKeys.Add(TestJwt.SigningKey);
                options.TokenValidationParameters.IssuerSigningKey = TestJwt.SigningKey;
            });
        });
    }

    private Task AddAsync(string host, FakeSource source)
    {
        _sources.Add(source);
        _hosts[host] = source.Handler;
        return Task.CompletedTask;
    }
}
