using System.Globalization;
using FluentValidation;

namespace Notifications.Application.Settings.ChangeSettings;

internal sealed class ChangeSettingsValidator : AbstractValidator<ChangeSettingsCommand>
{
    public ChangeSettingsValidator()
    {
        When(c => c.QuietHours is not null, () =>
        {
            RuleFor(c => c.QuietHours!.From).Must(BeTime).WithMessage("Podaj godzinę w formacie GG:MM.");
            RuleFor(c => c.QuietHours!.To).Must(BeTime).WithMessage("Podaj godzinę w formacie GG:MM.");
            RuleFor(c => c.QuietHours!.TimeZone).NotEmpty().MaximumLength(64);
        });
    }

    internal static bool BeTime(string? value) =>
        TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
