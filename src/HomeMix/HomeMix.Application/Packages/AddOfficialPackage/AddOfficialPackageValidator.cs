using FluentValidation;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Packages.AddOfficialPackage;

internal sealed class AddOfficialPackageValidator : AbstractValidator<AddOfficialPackageCommand>
{
    public AddOfficialPackageValidator() =>
        RuleFor(c => c.AddonId).Must(AddonManifestParser.IsValidId).WithMessage("Unknown add-on id.");
}
