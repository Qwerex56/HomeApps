using FluentResults;
using MediatR;

namespace Household.Api.Services.Invite.Commands.CreateInvite;

public record CreateInviteCommand(Guid HouseholdId, Guid InviterId) : IRequest<Result>;