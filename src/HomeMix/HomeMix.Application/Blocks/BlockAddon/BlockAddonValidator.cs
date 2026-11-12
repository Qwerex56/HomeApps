using FluentValidation;
using HomeMix.Domain.Blocks;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Blocks.BlockAddon;

internal sealed class BlockAddonValidator : AbstractValidator<BlockAddonCommand>
{
    public BlockAddonValidator()
    {
        RuleFor(c => c.Value)
            .NotEmpty()
            .Must(v => AddonBlock.IsSha256(v) || AddonManifestParser.IsValidId(v))
            .WithMessage("Give an add-on id (e.g. pomodoro) or a package SHA-256.");
        RuleFor(c => c.Reason).MaximumLength(AddonBlock.MaxReasonLength);
    }
}
