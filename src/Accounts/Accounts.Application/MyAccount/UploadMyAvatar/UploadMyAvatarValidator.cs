using Accounts.Application.Common;
using FluentValidation;

namespace Accounts.Application.MyAccount;

internal sealed class UploadMyAvatarValidator : AbstractValidator<UploadMyAvatarCommand>
{
    public UploadMyAvatarValidator() => RuleFor(c => c.Content).NotNull();
}
