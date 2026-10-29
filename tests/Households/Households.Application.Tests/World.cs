using Households.Application.AccountsSync;
using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Application.Households;
using Households.Application.Invites;
using Households.Domain.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Domain.Errors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Households.Application.Tests;

/// <summary>
/// The whole Application in memory: the real MediatR pipeline (validation, transaction) and domain event handlers,
/// ports over lists. Every request goes through <see cref="Send{T}"/> as the Api would send it.
/// </summary>
internal sealed class World : IDisposable
{
    public static readonly DateTimeOffset Start = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;

    public World()
    {
        Households = new InMemoryHouseholds(Users);
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<TimeProvider>(Time);
        services.AddSingleton(Settings);
        services.AddHouseholdsApplication();
        services.AddSingleton<IHouseholdRepository>(Households);
        services.AddSingleton<IHouseholdDirectory>(Households);
        services.AddSingleton<IInviteRepository>(Invites);
        services.AddSingleton<IUserCopies>(Users);
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IEventBus>(Bus);
        services.AddSingleton<FakeUnitOfWork>(sp => new FakeUnitOfWork(Households, sp));
        services.AddSingleton<IUnitOfWork>(sp => sp.GetRequiredService<FakeUnitOfWork>());
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public FixedTime Time { get; } = new(Start);

    public HouseholdSettings Settings { get; } = new();

    public InMemoryUsers Users { get; } = new();

    public InMemoryHouseholds Households { get; }

    public InMemoryInvites Invites { get; } = new();

    public FakeCurrentUser CurrentUser { get; } = new();

    public FakeEventBus Bus { get; } = new();

    public IServiceProvider Services => _provider;

    public int Saves => _provider.GetRequiredService<FakeUnitOfWork>().Saves;

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    /// <summary>A user known from Accounts (S1), signed in.</summary>
    public async Task<Guid> SignUp(string name = "Adam", ServerRole role = ServerRole.User)
    {
        var id = Guid.NewGuid();
        Assert.True((await Send(new CreateUserCopyCommand(id, name, null))).IsSuccess);
        SignInAs(id, role);
        return id;
    }

    public void SignInAs(Guid id, ServerRole role = ServerRole.User)
    {
        CurrentUser.UserId = id;
        CurrentUser.Role = role;
    }

    /// <summary>
    /// A household where <see cref="Family.Owner"/> invited an Admin, a Member and a Guest (each signed up in turn, an
    /// hour apart). The Owner is signed in at the end; published messages are cleared.
    /// </summary>
    public async Task<Family> Family(string name = "Dom")
    {
        var owner = await SignUp("Ola");
        var id = (await Send(new CreateHouseholdCommand(name, null))).Value;
        var admin = await Join(owner, id, "Adam", HouseholdRole.Member);
        SignInAs(owner);
        Assert.True((await Send(new Members.ChangeMemberRoleCommand(id, admin, HouseholdRole.Admin))).IsSuccess);
        var member = await Join(owner, id, "Marek", HouseholdRole.Member);
        var guest = await Join(owner, id, "Gosia", HouseholdRole.Guest);
        SignInAs(owner);
        Bus.Published.Clear();
        return new Family(id, owner, admin, member, guest);
    }

    /// <summary><paramref name="inviter"/> creates an invite and a new user accepts it.</summary>
    public async Task<Guid> Join(Guid inviter, Guid householdId, string name, HouseholdRole role, DateTimeOffset? guestAccessUntil = null)
    {
        Time.Advance(TimeSpan.FromHours(1));
        SignInAs(inviter);
        var invite = (await Send(new CreateInviteCommand(householdId, role, InviteValidity.SevenDays, guestAccessUntil))).Value;
        var id = await SignUp(name);
        Assert.True((await Send(new AcceptInviteCommand(invite.Code))).IsSuccess);
        return id;
    }

    public Household Household(Guid id) => Households.Items.Single(h => h.Id == id);

    public void Dispose() => _provider.Dispose();
}

internal sealed record Family(Guid Id, Guid Owner, Guid Admin, Guid Member, Guid Guest);

internal static class ResultExtensions
{
    public static string Code(this ResultBase result) => (string)result.Errors[0].Metadata[AppError.CodeKey];
}
