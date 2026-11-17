using FluentValidation;
using HomeMix.Domain.Boards;

namespace HomeMix.Application.Boards.SaveBoard;

internal sealed class SaveBoardValidator : AbstractValidator<SaveBoardCommand>
{
    public SaveBoardValidator()
    {
        RuleFor(c => c.HouseholdId).NotEmpty();
        RuleFor(c => c.Place).NotEmpty().MaximumLength(16);
        RuleFor(c => c.Items).NotNull();
        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Key).NotEmpty().MaximumLength(WidgetBoard.MaxKeyLength);
            item.RuleFor(i => i.Fit).MaximumLength(16);
        });
    }
}
