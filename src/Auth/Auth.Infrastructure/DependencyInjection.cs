using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Infrastructure.Emails;
using Auth.Infrastructure.External;
using Auth.Infrastructure.Jobs;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Security;
using HomeApps.BuildingBlocks.Infrastructure;
using HomeApps.BuildingBlocks.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// The adapters behind Application's ports: Postgres with the outbox, the consumers in this assembly, password
    /// hashing, token signing, Google, email and the background jobs. The Api calls <c>AddAuthApplication</c> next to it.
    /// </summary>
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddHomeAppsInfrastructure<AuthDbContext>(configuration, "auth", typeof(DependencyInjection).Assembly);

        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddValidatedOptions<SigningOptions>(SigningOptions.SectionName);
        services.AddSingleton<SigningKeys>();
        services.AddSingleton<ITokenIssuer, TokenIssuer>();
        services.AddOptions<ClientsOptions>()
            .BindConfiguration(ClientsOptions.SectionName)
            .Validate(o => o.Clients.Count > 0 && o.Clients.All(c => !string.IsNullOrWhiteSpace(c.ClientId)),
                "Auth:Clients needs at least one client, each with a ClientId.")
            .ValidateOnStart();
        services.AddSingleton<IClientRegistry, ClientRegistry>();

        services.AddOptions<GoogleOptions>().BindConfiguration(GoogleOptions.SectionName);
        services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(sp =>
            new ConfigurationManager<OpenIdConnectConfiguration>(
                sp.GetRequiredService<IOptions<GoogleOptions>>().Value.MetadataAddress.ToString(),
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever()));
        services.AddHttpClient<IExternalIdentityVerifier, ExternalIdentityVerifier>();

        services.AddEmail(configuration, environment);

        services.AddHostedService<BootstrapOnStart>();
        services.AddHostedService<DailyCleanup>();

        return services;
    }

    private static void AddEmail(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddValidatedOptions<EmailOptions>(EmailOptions.SectionName);

        // Q11: Development may run without SMTP and logs the emails; anywhere else SMTP is required.
        var smtp = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()?.Smtp ?? new SmtpOptions();
        if (smtp.IsConfigured)
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else if (environment.IsDevelopment())
        {
            services.AddSingleton<IEmailSender, LogEmailSender>();
        }
        else
        {
            throw new InvalidOperationException($"{EmailOptions.SectionName}:Smtp:Host is required outside Development.");
        }
    }
}
