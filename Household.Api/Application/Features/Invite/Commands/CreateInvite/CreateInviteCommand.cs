using FluentResults;
using MediatR;

namespace HouseholdService.Application.Features.Invite.Commands.CreateInvite;

public record CreateInviteCommand(Guid HouseholdId, Guid InviterId) : IRequest<Result>;