using System.Text.Json;
using FluentValidation;

namespace HomeMix.Application.Data.PutDocument;

internal sealed class PutDocumentValidator : AbstractValidator<PutDocumentCommand>
{
    public PutDocumentValidator()
    {
        RuleFor(c => c.InstallationId).NotEmpty();
        RuleFor(c => c.Scope).IsInEnum();
        RuleFor(c => c.Collection).DataName();
        RuleFor(c => c.Key).DataName();
        RuleFor(c => c.Value.ValueKind).NotEqual(JsonValueKind.Undefined).WithName("value").WithMessage("A value is required.");
        RuleFor(c => c.Version).GreaterThan(0);
    }
}
