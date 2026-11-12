using FluentValidation;

namespace HomeMix.Application.Packages.AddPackageFromZip;

internal sealed class AddPackageFromZipValidator : AbstractValidator<AddPackageFromZipCommand>
{
    public AddPackageFromZipValidator() => RuleFor(c => c.Zip).NotEmpty().WithMessage("Choose a zip file.");
}
