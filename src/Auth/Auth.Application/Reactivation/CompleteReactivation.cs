using Auth.Application.Abstractions;
using Auth.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Reactivation;

/// <summary>
/// A-09: exchanges the reactivation ticket for tokens once Accounts has enabled the account. Until then the answer is
/// 409 <c>reactivation.pending</c> and the Api adds <c>Retry-After</c> (Q5).
/// </summary>
public sealed record CompleteReactivationCommand(string Ticket, string ClientId, string? UserAgent) : ICommand<AuthTokens>;

internal sealed class CompleteReactivationValidator : AbstractValidator<CompleteReactivationCommand>
{
    public CompleteReactivationValidator()
    {
        RuleFor(c => c.Ticket).NotEmpty();
        RuleFor(c => c.ClientId).NotEmpty();
    }
}

internal sealed class CompleteReactivationHandler(
    IIdentityRepository identities,
    IClientRegistry clients,
    ITokenIssuer tokens,
    TimeProvider time) : ICommandHandler<CompleteReactivationCommand, AuthTokens>
{
    public async Task<Result<AuthTokens>> Handle(CompleteReactivationCommand command, CancellationToken cancellationToken)
    {
        var client = clients.Resolve(command.ClientId, command.UserAgent);
        if (client.IsFailed)
        {
            return client.ToResult<AuthTokens>();
        }

        if (tokens.ReadTicket(command.Ticket) is not ReactivationTicket ticket)
        {
            return Result.Fail<AuthTokens>(ApplicationErrors.TicketInvalid);
        }

        var identity = await identities.GetByIdAsync(ticket.IdentityId, cancellationToken);
        if (identity is null)
        {
            return Result.Fail<AuthTokens>(ApplicationErrors.TicketInvalid);
        }

        var issued = identity.CompleteReactivation(ticket.Method, client.Value.Info, time);
        return issued.IsFailed
            ? issued.ToResult<AuthTokens>()
            : tokens.Tokens(identity, issued.Value, client.Value.Registered);
    }
}
