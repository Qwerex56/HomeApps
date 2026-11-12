using FluentValidation;

namespace HomeMix.Application.Data.GetDocument;

internal sealed class GetDocumentValidator : AbstractValidator<GetDocumentQuery>
{
    public GetDocumentValidator()
    {
        RuleFor(q => q.InstallationId).NotEmpty();
        RuleFor(q => q.Scope).IsInEnum();
        RuleFor(q => q.Collection).DataName();
        RuleFor(q => q.Key).DataName();
    }
}
