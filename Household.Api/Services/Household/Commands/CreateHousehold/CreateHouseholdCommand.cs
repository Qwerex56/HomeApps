using System.ComponentModel.DataAnnotations;
using FluentResults;
using MediatR;

namespace Household.Api.Services.Household.Commands.CreateHousehold;

public record CreateHouseholdCommand(Guid UserId, [MaxLength(32)] string Name, [MaxLength(120)] string Description = "")
    : IRequest<Result>;