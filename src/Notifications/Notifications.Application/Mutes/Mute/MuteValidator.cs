using FluentValidation;

namespace Notifications.Application.Mutes.Mute;

internal sealed class MuteValidator : AbstractValidator<MuteCommand>
{
    public MuteValidator()
    {
        RuleFor(c => c.HouseholdId).NotEmpty();
        RuleFor(c => c.Source).MaximumLength(Domain.Recipients.Mute.MaxSourceLength);
    }
}
