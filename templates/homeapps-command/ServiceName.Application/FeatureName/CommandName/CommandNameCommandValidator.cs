using FluentValidation;

namespace ServiceName.Application.FeatureName.CommandName;

/// <summary>Shape checks only (required, lengths, formats). Business rules belong in the aggregate.</summary>
internal sealed class CommandNameCommandValidator : AbstractValidator<CommandNameCommand>
{
    public CommandNameCommandValidator()
    {
    }
}
