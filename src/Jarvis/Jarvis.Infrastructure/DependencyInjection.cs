using HomeApps.BuildingBlocks.Infrastructure;
using Jarvis.Infrastructure.Live;
using Jarvis.Infrastructure.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Gateway;
using Jarvis.Infrastructure.Brain;
using Jarvis.Infrastructure.Gateway;
using Jarvis.Infrastructure.Jobs;
using Jarvis.Infrastructure.Persistence;
using Jarvis.Infrastructure.Tools;
using Jarvis.Infrastructure.Voice;
using Jarvis.Application.Voice;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Postgres with the outbox and the consumers (<c>AssistantInvoked</c>, <c>AssistantConfirmed</c>, <c>UserErased</c>),
    /// the gateway's adapters (Auth's delegation, the sources, MCP clients), the OpenAI-compatible brain, the speech engines
    /// (OpenAI audio API, jarvis-voice-bridge.md) and the retention job.
    /// </summary>
    public static IServiceCollection AddJarvisInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddHomeAppsInfrastructure<JarvisDbContext>(configuration, "jarvis", typeof(DependencyInjection).Assembly);
        services.AddScoped<IRunRepository, RunRepository>();
        services.AddScoped<IToolCallRepository, ToolCallRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddSingleton<RunFeed>();
        services.AddSingleton<IRunFeed>(sp => sp.GetRequiredService<RunFeed>());

        services.AddOptions<GatewayOptions>()
            .BindConfiguration(GatewayOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddMemoryCache();
        services.AddHttpClient<IDelegation, AuthDelegation>((sp, http) =>
            http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<GatewayOptions>>().Value.AuthUrl.TrimEnd('/') + "/"));
        services.AddHttpClient<IToolSources, ConfiguredToolSources>();
        services.AddHttpClient(SourceConnector.HttpClientName);
        services.AddSingleton<SourceConnector>();
        services.AddScoped<IToolExecutor, GatewayToolExecutor>();

        services.AddOptions<OpenAiChatOptions>().BindConfiguration(OpenAiChatOptions.SectionName);
        services.AddDataProtection()
            .SetApplicationName("homeapps-jarvis")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(configuration["Jarvis:DataProtection:KeysPath"] ?? ".keys/dataprotection")));
        services.AddScoped<LlmSettingsStore>();
        services.AddScoped<ILlmConnection>(sp => sp.GetRequiredService<LlmSettingsStore>());
        services.AddHttpClient(OpenAiChatBrain.HttpClientName);
        services.AddScoped<IAssistantBrain, OpenAiChatBrain>();
        services.AddHttpClient<OpenAiModelHealthCheck>();
        services.AddHostedService(sp => sp.GetRequiredService<OpenAiModelHealthCheck>());

        services.AddHttpClient<ISpeechToText, OpenAiSpeechToText>((sp, http) => Engine(sp, http, v => v.SttUrl));
        services.AddHttpClient<ITextToSpeech, OpenAiTextToSpeech>((sp, http) => Engine(sp, http, v => v.TtsUrl));
        services.AddHttpClient<ISpeakers, HouseholdSpeakers>();
        services.AddHttpClient<IHouseholdCeilings, HouseholdsCeilings>(http => http.Timeout = TimeSpan.FromSeconds(3));
        services.AddHttpClient<IHouseholdDevices, HouseholdDevices>(http => http.Timeout = TimeSpan.FromSeconds(3));
        services.AddSingleton<IVoicePrints, NoVoicePrints>();

        services.AddHostedService<RetentionJob>();
        services.AddSingleton<InvocationQueue>();
        services.AddHostedService<InvocationWorker>();
        return services;
    }

    /// <summary>A speech engine at its configured URL, with the key for a cloud one; long enough for a slow CPU.</summary>
    private static void Engine(IServiceProvider services, HttpClient http, Func<VoiceOptions, string> url)
    {
        var voice = services.GetRequiredService<IOptions<VoiceOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(url(voice)))
        {
            http.BaseAddress = new Uri(url(voice).TrimEnd('/') + "/");
        }

        if (!string.IsNullOrWhiteSpace(voice.ApiKey))
        {
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", voice.ApiKey);
        }

        http.Timeout = TimeSpan.FromSeconds(60);
    }
}
