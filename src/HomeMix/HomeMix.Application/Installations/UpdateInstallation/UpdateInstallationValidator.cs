using FluentValidation;

namespace HomeMix.Application.Installations.UpdateInstallation;

internal sealed class UpdateInstallationValidator : AbstractValidator<UpdateInstallationCommand>
{
    public UpdateInstallationValidator()
    {
        RuleFor(c => c.InstallationId).NotEmpty();
        RuleFor(c => c.Sha256).NotEmpty().Length(64);
    }
}
