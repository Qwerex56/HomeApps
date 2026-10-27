using Households.Domain.Common;
using Households.Domain.Households;
using Households.Domain.Invites;

namespace Households.Domain.Tests;

internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

internal static class A
{
    public static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    public const int MaxMembers = 100;

    public static Actor User(Guid id) => new(id, ServerRole.User);

    public static Actor ServerAdmin() => new(Guid.NewGuid(), ServerRole.Admin);

    public static string Code(this FluentResults.ResultBase result) =>
        (string)result.Errors[0].Metadata[HomeApps.BuildingBlocks.Domain.Errors.AppError.CodeKey];
}

/// <summary>A household with an Owner, an Admin, a Member and a Guest who joined one after another (an hour apart).</summary>
internal sealed class AHousehold
{
    public AHousehold(FixedTime time)
    {
        Time = time;
        Household = Household.Create(Guid.NewGuid(), Owner.Id, "Dom", null, time).Value;
        Add(Admin.Id, HouseholdRole.Admin);
        Add(Member.Id, HouseholdRole.Member);
        Add(Guest.Id, HouseholdRole.Guest);
        Household.ClearDomainEvents();
    }

    public FixedTime Time { get; }

    public Household Household { get; }

    public Actor Owner { get; } = A.User(Guid.NewGuid());

    public Actor Admin { get; } = A.User(Guid.NewGuid());

    public Actor Member { get; } = A.User(Guid.NewGuid());

    public Actor Guest { get; } = A.User(Guid.NewGuid());

    public Actor Stranger { get; } = A.User(Guid.NewGuid());

    public Actor Of(string who) => who switch
    {
        "owner" => Owner,
        "admin" => Admin,
        "member" => Member,
        "guest" => Guest,
        "stranger" => Stranger,
        "server-admin" => A.ServerAdmin(),
        _ => throw new ArgumentOutOfRangeException(nameof(who)),
    };

    /// <summary>Joins through a fresh invite from the Owner, then sets the role directly through the Owner.</summary>
    public Guid Add(Guid userId, HouseholdRole role, DateTimeOffset? guestAccessUntil = null)
    {
        Time.Advance(TimeSpan.FromHours(1));
        var inviteRole = role == HouseholdRole.Guest ? HouseholdRole.Guest : HouseholdRole.Member;
        var invite = Household.CreateInvite(Owner, inviteRole, InviteValidity.SevenDays, guestAccessUntil, Time).Value;
        Assert.True(Household.Accept(invite, userId, A.MaxMembers, Time).IsSuccess);
        if (role == HouseholdRole.Admin)
        {
            Assert.True(Household.ChangeRole(Owner, userId, HouseholdRole.Admin, Time).IsSuccess);
        }

        return userId;
    }

    public HouseholdRole RoleOf(Actor actor) => Household.MemberOf(actor.Id)!.Role;
}
