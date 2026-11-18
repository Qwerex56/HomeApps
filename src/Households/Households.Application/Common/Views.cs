using Households.Domain.Households;

namespace Households.Application.Common;

/// <summary>
/// H3. Parts depend on who asks (D2a): <see cref="MemberCount"/> only for members and up and server admins,
/// <see cref="GuestLink"/> only for the Owner and Admins, <see cref="OwnershipReceivedAt"/> only for the Owner (D5a).
/// <see cref="MyRole"/> is null for a server admin who is not a member.
/// </summary>
public sealed record HouseholdView(
    Guid Id,
    string Name,
    string? Description,
    HouseholdStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ArchivedAt,
    HouseholdRole? MyRole,
    int? MemberCount,
    string? GuestLink,
    DateTimeOffset? OwnershipReceivedAt);

/// <summary>H2.</summary>
public sealed record MyHouseholdView(Guid Id, string Name, string? Description, HouseholdRole MyRole, int? MemberCount, bool OwnershipReceived);

/// <summary>
/// M1. <see cref="Name"/> is the nickname or else the display name from Accounts (Q4). <see cref="IsActive"/> false:
/// shown greyed out as "account inactive" (D6). <see cref="InvitedBy"/> per D3c.
/// </summary>
public sealed record MemberView(
    Guid UserId,
    string Name,
    string? Nickname,
    string DisplayName,
    string? AvatarUrl,
    HouseholdRole Role,
    bool IsActive,
    DateTimeOffset JoinedAt,
    Guid? InvitedBy,
    string? InvitedByName,
    DateTimeOffset? AccessUntil);

/// <summary>S5: my membership, for the front and for other services asking about the caller.</summary>
public sealed record MyMembershipView(Guid HouseholdId, HouseholdRole Role, string? Nickname, DateTimeOffset JoinedAt, DateTimeOffset? AccessUntil);

/// <summary>I1, I2. <see cref="Code"/> as people see it (<c>ABCDE-FGHJK</c>, Q1).</summary>
public sealed record InviteView(
    Guid Id,
    string Code,
    HouseholdRole Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? GuestAccessUntil,
    int Uses,
    Guid? CreatedBy,
    string? CreatedByName);

/// <summary>I4: what the person opening a link sees before joining. <see cref="InvitedByName"/> is null for the guest link and after Q6.</summary>
public sealed record InvitePreviewView(Guid HouseholdId, string HouseholdName, HouseholdRole Role, string? InvitedByName, DateTimeOffset? ExpiresAt);

/// <summary>Q7.</summary>
public sealed record AdminHouseholdView(
    Guid Id,
    string Name,
    HouseholdStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ArchivedAt,
    int MemberCount,
    Guid? OwnerId,
    string? OwnerName);
