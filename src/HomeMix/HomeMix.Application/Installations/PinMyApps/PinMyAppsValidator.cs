using FluentValidation;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations.PinMyApps;

internal sealed class PinMyAppsValidator : AbstractValidator<PinMyAppsCommand>
{
    public PinMyAppsValidator()
    {
        RuleFor(c => c.InstallationIds).NotNull()
            .Must(ids => ids.Count <= Installation.MaxPins).WithMessage($"At most {Installation.MaxPins} add-ons.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Every add-on once.");
    }
}
