using FluentValidation;

namespace Devices.Application.Tools.CallTool;

internal sealed class CallToolValidator : AbstractValidator<CallToolCommand>
{
    public CallToolValidator() => RuleFor(c => c.Tool).NotEmpty().MaximumLength(100);
}
