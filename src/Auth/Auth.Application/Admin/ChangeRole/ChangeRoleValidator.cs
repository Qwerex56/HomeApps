using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Admin;

internal sealed class ChangeRoleValidator : AbstractValidator<ChangeRoleCommand>
{
    public ChangeRoleValidator() => RuleFor(c => c.Role).IsInEnum();
}
