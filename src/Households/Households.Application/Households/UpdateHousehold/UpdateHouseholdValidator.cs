using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

internal sealed class UpdateHouseholdValidator : AbstractValidator<UpdateHouseholdCommand>
{
    public UpdateHouseholdValidator() => RuleFor(c => c.Name).NotNull();
}
