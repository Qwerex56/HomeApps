using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Common;

namespace Jarvis.Application.Actions.ListPendingConfirmations;

/// <summary>
/// Lists the caller's own pending confirmations (newest first).
/// </summary>
public sealed record ListPendingConfirmationsQuery(Guid UserId) : IQuery<IReadOnlyList<ActionView>>;
