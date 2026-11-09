using HomeApps.Contracts.Households;

namespace HomeApps.Plugins;

/// <summary>The caller's place in a household, as Households reports it (<c>GET /households/{id}/members/me</c>).</summary>
/// <param name="JoinedAt">When they joined this time; a guest who left and came back has the later date. A plugin
/// can hide what happened before it (the chat shows guests messages only from then on).</param>
/// <param name="AccessUntil">When a guest's access ends; <c>null</c> for members and for guests without an end.</param>
public sealed record HouseholdMembership(HouseholdRole Role, DateTimeOffset JoinedAt, DateTimeOffset? AccessUntil);
