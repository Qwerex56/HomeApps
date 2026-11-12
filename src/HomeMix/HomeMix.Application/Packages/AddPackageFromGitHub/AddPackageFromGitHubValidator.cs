using FluentValidation;

namespace HomeMix.Application.Packages.AddPackageFromGitHub;

internal sealed class AddPackageFromGitHubValidator : AbstractValidator<AddPackageFromGitHubCommand>
{
    public AddPackageFromGitHubValidator()
    {
        RuleFor(c => c.Repository)
            .NotEmpty()
            .MaximumLength(300)
            .Must(r => Uri.TryCreate(r, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
                && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath.Trim('/').Split('/').Length >= 2)
            .WithMessage("Paste a GitHub repository link such as https://github.com/owner/repo.");
        RuleFor(c => c.Tag).MaximumLength(100);
    }
}
