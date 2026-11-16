using System.Net.Http.Json;
using HomeMix.Application;
using HomeMix.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMix.Infrastructure.Store;

/// <summary>
/// Reads <c>addons.json</c> (<c>HomeMix:StoreIndexUrl</c>): the repositories of the official store entries. An index
/// that cannot be read lists nothing; the entries already in the store stay.
/// </summary>
internal sealed partial class GitHubStoreIndex(HttpClient http, IOptions<HomeMixOptions> options, ILogger<GitHubStoreIndex> logger) : IStoreIndex
{
    /// <summary>At most this many official repositories, to stay under GitHub's anonymous limit.</summary>
    public const int MaxRepositories = 30;

    public async Task<IReadOnlyList<string>> RepositoriesAsync(CancellationToken cancellationToken)
    {
        var url = options.Value.StoreIndexUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            return [];
        }

        try
        {
            var index = await http.GetFromJsonAsync<StoreIndexFile>(new Uri(url), cancellationToken);
            return [.. (index?.Addons ?? [])
                .Select(a => GitHubRepositoriesClient.Normalize(a.Repository))
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxRepositories)];
        }
        catch (Exception e) when (e is HttpRequestException or System.Text.Json.JsonException or TaskCanceledException)
        {
            LogIndexFailed(logger, url, e.Message);
            return [];
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The HomeMix store index {Url} could not be read: {Problem}")]
    private static partial void LogIndexFailed(ILogger logger, string url, string problem);
}
