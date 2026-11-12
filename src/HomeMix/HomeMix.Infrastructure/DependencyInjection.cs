using HomeApps.BuildingBlocks.Infrastructure;
using HomeApps.BuildingBlocks.Infrastructure.Options;
using HomeMix.Application;
using HomeMix.Application.Abstractions;
using HomeMix.Infrastructure.GitHub;
using HomeMix.Infrastructure.Households;
using HomeMix.Infrastructure.Persistence;
using HomeMix.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomeMix.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// The adapters behind Application's ports: Postgres with the outbox, the zips on the <c>homemix-packages</c>
    /// volume, GitHub releases and household roles from Households (section <c>HomeMix</c>). The Api calls
    /// <c>AddHomeMixApplication</c> next to it.
    /// </summary>
    public static IServiceCollection AddHomeMixInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHomeAppsInfrastructure<HomeMixDbContext>(configuration, "homemix", typeof(DependencyInjection).Assembly);
        services.AddValidatedOptions<HomeMixOptions>(HomeMixOptions.SectionName);
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IInstallationRepository, InstallationRepository>();

        services.AddSingleton<IPackageReader, ZipPackageReader>();
        services.AddSingleton<IPackageStore, FilePackageStore>();

        services.AddHttpClient<IGitHubReleases, GitHubReleasesClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<HomeMixOptions>>().Value;
            http.BaseAddress = new Uri(options.GitHubApiUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(30);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("HomeApps-HomeMix");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            if (!string.IsNullOrWhiteSpace(options.GitHubToken))
            {
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.GitHubToken);
            }
        });

        services.AddHttpClient<IHouseholdRoles, HouseholdsHttpRoles>((sp, http) =>
            http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<HomeMixOptions>>().Value.HouseholdsUrl.TrimEnd('/') + "/"));

        return services;
    }
}
