using FluentValidation;

namespace HomeMix.Application.Data.DeleteDocument;

internal sealed class DeleteDocumentValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentValidator()
    {
        RuleFor(c => c.InstallationId).NotEmpty();
        RuleFor(c => c.Scope).IsInEnum();
        RuleFor(c => c.Collection).DataName();
        RuleFor(c => c.Key).DataName();
        RuleFor(c => c.Version).GreaterThan(0);
    }
}
