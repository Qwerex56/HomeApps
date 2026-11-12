using FluentValidation;
using HomeMix.Domain.Data;

namespace HomeMix.Application.Data;

/// <summary>Validation shared by the data requests.</summary>
internal static class DataRequestRules
{
    public static IRuleBuilderOptions<T, string> DataName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(DataNames.IsValid)
            .WithMessage($"Use 1-{DataNames.MaxLength} letters, digits, '.', '_' or '-', starting with a letter or digit.");
}
