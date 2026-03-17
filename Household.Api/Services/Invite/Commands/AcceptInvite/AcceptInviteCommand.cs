using MediatR;

namespace Household.Api.Services.Invite.Commands.AcceptInvite;

public record AcceptInviteCommand(string InviteCode, Guid UserId) : IRequest;