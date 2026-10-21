using Auth.Application;
using Auth.Application.Abstractions;
using Auth.Infrastructure.Emails;
using HomeApps.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Auth.Infrastructure.Tests;

public sealed class WiringTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"auth-wiring-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private ServiceProvider Build(string environment, Dictionary<string, string?>? extra = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Database=auth_wiring",
            ["Messaging:Transport"] = "InMemory",
            ["Auth:Signing:Issuer"] = "https://auth.homeapps.test",
            ["Auth:Clients:0:ClientId"] = "homeapps-web",
            ["Auth:Clients:0:Name"] = "HomeApps",
            ["Auth:Clients:0:Type"] = "Browser",
            ["Auth:Email:FrontendBaseUrl"] = "https://home.example.com",
        };
        foreach (var (key, value) in extra ?? [])
        {
            settings[key] = value;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(Substitute.For<ICurrentUser>());
        services.AddAuthApplication(configuration);
        var hostEnvironment = new TestEnvironment(environment, _root);
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(hostEnvironment);
        services.AddAuthInfrastructure(configuration, hostEnvironment);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Every_handler_and_port_resolves_in_development()
    {
        await using var provider = Build("Development");
        await using var scope = provider.CreateAsyncScope();

        var handlerTypes = typeof(AuthOptions).Assembly.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                || i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .Distinct();
        Assert.All(handlerTypes, t => Assert.NotEmpty(scope.ServiceProvider.GetServices(t)));

        Assert.IsType<LogEmailSender>(scope.ServiceProvider.GetRequiredService<IEmailSender>());
        Assert.NotNull(scope.ServiceProvider.GetService<Accounts.UserDisabledConsumer>());
        Assert.NotNull(scope.ServiceProvider.GetService<AuthEmailConsumer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IClientRegistry>().Find("homeapps-web"));
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IExternalIdentityVerifier>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IOptions<EmailOptions>>().Value.FrontendBaseUrl);
    }

    [Fact]
    public async Task Production_needs_smtp()
    {
        Assert.Throws<InvalidOperationException>(() => Build("Production"));

        await using var provider = Build("Production", new() { ["Auth:Email:Smtp:Host"] = "smtp.example.com" });
        Assert.IsType<SmtpEmailSender>(provider.GetRequiredService<IEmailSender>());
    }
}
