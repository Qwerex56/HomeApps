using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Common;

namespace Jarvis.Application.Actions.Decide;

/// <summary>"Potwierdź" or "Anuluj" on the action card (JV9): only the person who asked, within 10 minutes.</summary>
public sealed record DecideCommand(Guid UserId, Guid ActionId, bool Approve) : ICommand<ActionView>;
