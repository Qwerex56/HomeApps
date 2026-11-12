using FluentValidation;

namespace HomeMix.Application.Data.ListDocuments;

internal sealed class ListDocumentsValidator : AbstractValidator<ListDocumentsQuery>
{
    public ListDocumentsValidator()
    {
        RuleFor(q => q.InstallationId).NotEmpty();
        RuleFor(q => q.Scope).IsInEnum();
        RuleFor(q => q.Collection).DataName();
    }
}
