using Accounts.Application.Admin.GetServerAbout;

namespace Accounts.Application.Abstractions;

/// <summary>What "About the server" reads from outside Accounts: the other services, the plugins and GitHub.</summary>
public interface IServerInfo
{
    /// <summary>Every service, Accounts included, from its <c>/health/version</c>; one that does not answer has no version.</summary>
    Task<IReadOnlyList<ServiceAboutView>> ServicesAsync(CancellationToken cancellationToken);

    /// <summary>The configured plugins with their version; one that does not answer is <c>unavailable</c>.</summary>
    Task<IReadOnlyList<PluginAboutView>> PluginsAsync(CancellationToken cancellationToken);

    /// <summary>The tag of the newest release (cached for an hour), or <c>null</c> when GitHub could not be reached in 3 seconds.</summary>
    Task<string?> LatestReleaseTagAsync(CancellationToken cancellationToken);
}
