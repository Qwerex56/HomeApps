using FluentResults;
using MediatR;

namespace HouseholdService.Application.Features.Invite.Commands.RevokeInvite;

public record RevokeInviteCommand(Guid InviteId, Guid UserId) : IRequest<Result>;
