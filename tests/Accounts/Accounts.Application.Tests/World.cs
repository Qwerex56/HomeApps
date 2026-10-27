using Accounts.Application.AuthSync;
using Accounts.Domain.Common;
using Accounts.Domain.Users;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using Contracts = HomeApps.Contracts.Auth;

namespace Accounts.Application.Tests;

/// <summary>Everything a handler needs, in memory. Handlers are called directly; the pipeline has its own test.</summary>
internal sealed class World
{
    public static readonly DateTimeOffset Start = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    public FixedTime Time { get; } = new(Start);

    public InMemoryUsers Users { get; } = new();

    public FakeCurrentUser CurrentUser { get; } = new();

    public FakeEventBus Bus { get; } = new();

    public static Contracts.IdentityRegistered Identity(
        Guid? id = null,
        string email = "adam@example.com",
        string displayName = "Adam",
        Contracts.ServerRole role = Contracts.ServerRole.User) =>
        new(
            id ?? Guid.NewGuid(), email, role, displayName, "Adam", "Nowak", null,
            Contracts.RegistrationSource.Password, Start, false, null, null,
            new Contracts.RegistrationContext("homeapps-web", "Firefox", "pl-PL"));

    /// <summary>A registered, active user, as created from Auth's event; domain events cleared.</summary>
    public async Task<User> Registered(
        string email = "adam@example.com",
        string displayName = "Adam",
        Contracts.ServerRole role = Contracts.ServerRole.User)
    {
        var identity = Identity(email: email, displayName: displayName, role: role);
        await new RegisterUserHandler(Users, Time).Handle(new RegisterUserCommand(identity), CancellationToken.None);
        var user = Users.Items.Single(u => u.Id == identity.UserId);
        user.ClearDomainEvents();
        return user;
    }

    /// <summary>Signs <paramref name="user"/> in as the current user.</summary>
    public void SignInAs(User user)
    {
        CurrentUser.UserId = user.Id;
        CurrentUser.Role = user.Role;
    }

    /// <summary>Signs in someone who has no account here, with the given role.</summary>
    public Guid SignInAsSomeone(ServerRole role)
    {
        CurrentUser.UserId = Guid.NewGuid();
        CurrentUser.Role = role;
        return CurrentUser.UserId.Value;
    }
}

internal static class ResultExtensions
{
    public static string Code(this ResultBase result) => (string)result.Errors[0].Metadata[AppError.CodeKey];
}
