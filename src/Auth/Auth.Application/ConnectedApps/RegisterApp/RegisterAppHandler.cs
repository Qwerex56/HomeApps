using Auth.Application.Abstractions;
using Auth.Domain.ConnectedApps;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Options;

namespace Auth.Application.ConnectedApps.RegisterApp;

internal sealed class RegisterAppHandler(
    IAppClientRepository clients,
    IUnitOfWork unitOfWork,
    IOptions<ConnectedAppsOptions> options,
    TimeProvider time) : ICommandHandler<RegisterAppCommand, RegisteredAppView>
{
    public async Task<Result<RegisteredAppView>> Handle(RegisterAppCommand command, CancellationToken cancellationToken)
    {
        if (options.Value.Resources.Count == 0)
        {
            return Result.Fail<RegisteredAppView>(ConnectedAppErrors.Disabled);
        }

        if (await clients.CountAsync(cancellationToken) >= options.Value.MaxClients)
        {
            return Result.Fail<RegisteredAppView>(ConnectedAppErrors.TooManyClients);
        }

        var client = AppClient.Register(command.ClientName, command.RedirectUris, time.GetUtcNow());
        if (client.IsFailed)
        {
            return client.ToResult<RegisteredAppView>();
        }

        clients.Add(client.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RegisteredAppView(client.Value.Id, client.Value.Name, client.Value.RedirectUris, client.Value.RegisteredAt);
    }
}
