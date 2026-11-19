using FluentValidation;

namespace Devices.Application.Controls.SetControlForAddon;

internal sealed class SetControlForAddonValidator : AbstractValidator<SetControlForAddonCommand>
{
    public SetControlForAddonValidator()
    {
        RuleFor(c => c.Control).NotEmpty().MaximumLength(61);
        RuleFor(c => c.AddonName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.HouseholdId).NotEmpty();
        RuleFor(c => c.InstallerUserId).NotEmpty();
        RuleFor(c => c.InstallationId).NotEmpty();
    }
}
