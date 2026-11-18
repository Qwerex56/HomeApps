using FluentValidation;

namespace Jarvis.Application.Voice.VoiceTurn;

internal sealed class VoiceTurnValidator : AbstractValidator<VoiceTurnCommand>
{
    public VoiceTurnValidator()
    {
        When(c => c.Text is null, () =>
        {
            RuleFor(c => c.SampleRate).InclusiveBetween(8_000, 48_000);
            RuleFor(c => c.Audio.Length).GreaterThan(0).Must(n => n % 2 == 0).WithMessage("Audio must be 16-bit samples.");
        });
        RuleFor(c => c.Text).MaximumLength(4000);
    }
}
