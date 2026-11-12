using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.Accounts.ForgetUser;

/// <summary><c>UserErased</c> (jarvis-v1.md 6): the person's log goes.</summary>
public sealed record ForgetUserCommand(Guid UserId) : ICommand;
