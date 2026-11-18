using FluentValidation;

namespace Devices.Application.Frames.ProcessFrames;

internal sealed class ProcessFramesValidator : AbstractValidator<ProcessFramesCommand>
{
    /// <summary>The most frames one report (or one batch) may carry.</summary>
    public const int MaxFrames = 50;

    public ProcessFramesValidator()
    {
        RuleFor(c => c.Frames).NotEmpty().Must(f => f.Count <= MaxFrames).WithMessage($"A report carries 1-{MaxFrames} frames.");
    }
}
