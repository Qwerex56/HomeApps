using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Common;
using Accounts.Domain.Users;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Auth;
using Registration = Accounts.Domain.Users.Registration;

namespace Accounts.Application.AuthSync;

/// <summary>
/// UC-08, UC-09: the account for a new identity, from Auth's <c>IdentityRegistered</c>. Idempotent (R7): an existing
/// or erased id changes nothing. Never fails on profile data: unusable values are cleaned (see <see cref="Profile.FromRegistration"/>).
/// </summary>
public sealed record RegisterUserCommand(IdentityRegistered Identity) : ICommand;

internal sealed class RegisterUserHandler(IUserRepository users, TimeProvider time) : ICommandHandler<RegisterUserCommand>
{
    public async Task<Result> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var e = command.Identity;
        if (await users.GetByIdAsync(e.UserId, cancellationToken) is not null
            || await users.IsErasedAsync(e.UserId, cancellationToken))
        {
            return Result.Ok();
        }

        var email = Email.Create(e.Email);
        if (email.IsFailed)
        {
            return email.ToResult();
        }

        var profile = Profile.FromRegistration(e.DisplayName, e.FirstName, e.LastName, e.AvatarUrl, e.Context.Locale);
        var registration = Registration.Create(
            e.Source.ToDomain(), e.RegisteredAt, e.ViaReferral, e.InvitedByEmail, e.InvitedEmail,
            e.Context.ClientApp, e.Context.UserAgent, e.Context.Locale);

        users.Add(User.Register(e.UserId, email.Value, e.Role.ToDomain(), profile, registration, time));
        return Result.Ok();
    }
}
