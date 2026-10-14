using FluentValidation;
using ServiceName.Domain.Echoes;

namespace ServiceName.Application.Echoes.CreateEcho;

internal sealed class CreateEchoCommandValidator : AbstractValidator<CreateEchoCommand>
{
    public CreateEchoCommandValidator()
    {
        RuleFor(c => c.Message).NotEmpty().MaximumLength(Echo.MaxMessageLength);
    }
}
