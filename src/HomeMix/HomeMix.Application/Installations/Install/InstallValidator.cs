using FluentValidation;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations.Install;

internal sealed class InstallValidator : AbstractValidator<InstallCommand>
{
    public InstallValidator()
    {
        RuleFor(c => c.Sha256).NotEmpty().Length(64);
        RuleFor(c => c.Target).IsInEnum();
        RuleFor(c => c.HouseholdId).NotEmpty().When(c => c.Target == TargetKind.Household)
            .WithMessage("Choose the household.");
        RuleFor(c => c.HouseholdId).Null().When(c => c.Target == TargetKind.User)
            .WithMessage("An add-on on an account belongs to no household.");
        RuleFor(c => c.Consent).NotNull();
    }
}
