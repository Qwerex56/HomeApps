using FluentValidation;

namespace Auth.Application.Devices.IdentifySpeaker;

internal sealed class IdentifySpeakerValidator : AbstractValidator<IdentifySpeakerCommand>
{
    /// <summary>Households has at most 100 members by default.</summary>
    public const int MaxCandidates = 100;

    public IdentifySpeakerValidator()
    {
        RuleFor(c => c.DeviceId).NotEmpty();
        RuleFor(c => c.Candidates).NotNull();
        RuleFor(c => c.Candidates.Count).InclusiveBetween(1, MaxCandidates).When(c => c.Candidates is not null);
        RuleFor(c => c.Pin).NotEmpty().MaximumLength(16);
    }
}
