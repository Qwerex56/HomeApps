using MediatR;

namespace HouseholdService.Application.Features.Invite.Commands.AcceptInvite;

public record AcceptInviteCommand(string InviteCode, Guid UserId) : IRequest;