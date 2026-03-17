using FluentResults;
using MediatR;

namespace Household.Api.Services.Household.Commands.UpdateHousehold;

public record UpdateHouseholdCommand(Guid Id, Guid EditorId, string NewName, string NewDescription) : IRequest<Result>;