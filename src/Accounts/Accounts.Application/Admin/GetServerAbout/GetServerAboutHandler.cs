using System.Reflection;
using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin.GetServerAbout;

internal sealed class GetServerAboutHandler(IServerInfo info, ICurrentUser currentUser) : IQueryHandler<GetServerAboutQuery, ServerAboutView>
{
    private const string ReleasesUrl = "https://github.com/qwerex56/HomeApps/releases/tag/";

    public async Task<Result<ServerAboutView>> Handle(GetServerAboutQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin())
        {
            return Result.Fail<ServerAboutView>(UserErrors.ForbiddenRole);
        }

        var current = (typeof(GetServerAboutHandler).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "0.0.0").Split('+')[0];
        var services = info.ServicesAsync(cancellationToken);
        var plugins = info.PluginsAsync(cancellationToken);
        var latestTag = await info.LatestReleaseTagAsync(cancellationToken);

        var latest = latestTag?.TrimStart('v', 'V');
        return new ServerAboutView(
            current,
            latest,
            IsNewer(latest, current),
            latestTag is null ? null : ReleasesUrl + latestTag,
            await services,
            await plugins);
    }

    /// <summary>Null when either version cannot be read; a pre-release counts as its release.</summary>
    private static bool? IsNewer(string? latest, string current) =>
        Version.TryParse(latest?.Split('-')[0], out var l) && Version.TryParse(current.Split('-')[0], out var c) ? l > c : null;
}
