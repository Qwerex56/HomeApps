using Auth.Application.Abstractions;
using Auth.Domain.ConnectedApps;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Options;

namespace Auth.Application.ConnectedApps.DecideConsent;

internal sealed class DecideConsentHandler(
    IAppClientRepository clients,
    IAuthorizationCodeRepository codes,
    IIdentityRepository identities,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IOptions<ConnectedAppsOptions> options,
    TimeProvider time) : ICommandHandler<DecideConsentCommand, ConsentRedirect>
{
    public async Task<Result<ConsentRedirect>> Handle(DecideConsentCommand command, CancellationToken cancellationToken)
    {
        var checkedRequest = await clients.CheckAsync(command.Request, options.Value, cancellationToken);
        if (checkedRequest.IsFailed)
        {
            return checkedRequest.ToResult<ConsentRedirect>();
        }

        var request = checkedRequest.Value;
        if (!command.Allow)
        {
            return new ConsentRedirect(RedirectLocation.With(request.RedirectUri, ("error", "access_denied"), ("state", request.State)));
        }

        var now = time.GetUtcNow();
        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (identity is null || !identity.CanSignIn(now))
        {
            return Result.Fail<ConsentRedirect>(IdentityErrors.CannotSignIn);
        }

        var (code, raw) = AuthorizationCode.Issue(
            request.Client.Id, identity.Id, request.RedirectUri, request.CodeChallenge, request.Audience, now);
        codes.Add(code);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ConsentRedirect(RedirectLocation.With(request.RedirectUri, ("code", raw), ("state", request.State)));
    }
}
