using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

internal sealed class EraseUserValidator : AbstractValidator<EraseUserCommand>
{
    public EraseUserValidator() => RuleFor(c => c.ConfirmEmail).NotEmpty();
}
