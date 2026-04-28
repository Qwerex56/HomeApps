using FluentResults;
using MediatR;

namespace HouseholdService.Application.Features.Household.Commands.UpdateHousehold;

public record UpdateHouseholdCommand(Guid Id, Guid EditorId, string NewName, string NewDescription) : IRequest<Result>;