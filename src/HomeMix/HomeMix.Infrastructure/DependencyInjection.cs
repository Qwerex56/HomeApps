using HomeApps.BuildingBlocks.Infrastructure;
using HomeApps.BuildingBlocks.Infrastructure.Options;
using HomeMix.Application;
using HomeMix.Application.Abstractions;
using HomeMix.Infrastructure.Devices;
using HomeMix.Infrastructure.GitHub;
using HomeMix.Infrastructure.Households;
using HomeMix.Infrastructure.Jobs;
using HomeMix.Infrastructure.Persistence;
using HomeMix.Infrastructure.Storage;
using HomeMix.Infrastructure.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomeMix.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// The adapters behind Application's ports: Postgres with the outbox, the zips on the <c>homemix-packages</c>
    /// volume, GitHub releases, household roles from Households and the hourly cleanup of old add-on data (section <c>HomeMix</c>). The Api calls
    /// <c>AddHomeMixApplication</c> next to it.
    /// </summary>
    public static IServiceCollection AddHomeMixInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHomeAppsInfrastructure<HomeMixDbContext>(configuration, "homemix", typeof(DependencyInjection).Assembly);
        services.AddValidatedOptions<HomeMixOptions>(HomeMixOptions.SectionName);
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IInstallationRepository, InstallationRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IBlockRepository, BlockRepository>();
        services.AddScoped<IStoreListingRepository, StoreListingRepository>();
        services.AddScoped<IStoreStatsRepository, StoreStatsRepository>();
        services.AddHostedService<RemovedDataCleanup>();
        services.AddHostedService<StoreRefresh>();

        services.AddSingleton<IPackageReader, ZipPackageReader>();
        services.AddSingleton<IPackageStore, FilePackageStore>();

        services.AddSingleton<IOfficialAddons, FolderOfficialAddons>();
        services.AddHttpClient<IGitHubReleases, GitHubReleasesClient>(ConfigureGitHub);
        services.AddHttpClient<IStoreIndex, GitHubStoreIndex>(ConfigureGitHub);
        services.AddHttpClient<IGitHubRepositories, GitHubRepositoriesClient>(ConfigureGitHub);

        services.AddHttpClient<IHouseholdRoles, HouseholdsHttpRoles>((sp, http) =>
            http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<HomeMixOptions>>().Value.HouseholdsUrl.TrimEnd('/') + "/"));

        services.AddHttpClient<IHouseholdDevices, DevicesHttp>((sp, http) =>
            http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<HomeMixOptions>>().Value.DevicesUrl.TrimEnd('/') + "/"));

        // Triggers run with nobody logged in: Devices and Households are asked as HomeMix itself.
        services.AddHttpClient<IAddonDevices, DevicesServiceClient>((sp, http) => ConfigureServiceClient(sp, http, o => o.DevicesUrl));
        services.AddHttpClient<IHouseholdAdmins, HouseholdsServiceAdmins>((sp, http) => ConfigureServiceClient(sp, http, o => o.HouseholdsUrl));
        services.AddScoped<ITriggerStates, TriggerStateRepository>();

        return services;
    }

    private static void ConfigureServiceClient(IServiceProvider sp, HttpClient http, Func<HomeMixOptions, string> url)
    {
        var options = sp.GetRequiredService<IOptions<HomeMixOptions>>().Value;
        http.BaseAddress = new Uri(url(options).TrimEnd('/') + "/");
        http.Timeout = TimeSpan.FromSeconds(15);
        http.DefaultRequestHeaders.Add("X-Client-Id", options.ClientId);
        http.DefaultRequestHeaders.Add("X-Client-Secret", options.ClientSecret);
    }

    /// <summary>GitHub's REST API, with the optional token (<c>HomeMix:GitHubToken</c>).</summary>
    private static void ConfigureGitHub(IServiceProvider sp, HttpClient http)
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
    }
}
