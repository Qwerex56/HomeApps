using Auth.Application.Invitations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Jobs;

/// <summary>A-30: once per start, invite the configured Owner while there is none. A repeated start does nothing.</summary>
internal sealed partial class BootstrapOnStart(IServiceScopeFactory scopes, ILogger<BootstrapOnStart> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new EnsureBootstrapInvitationCommand(), stoppingToken);
        if (result.IsFailed)
        {
            LogFailed(logger, string.Join("; ", result.Errors.Select(e => e.Message)));
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not create the bootstrap invitation: {Errors}")]
    private static partial void LogFailed(ILogger logger, string errors);
}
