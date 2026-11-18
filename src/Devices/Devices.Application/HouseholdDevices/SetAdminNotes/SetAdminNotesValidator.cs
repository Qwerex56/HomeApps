using FluentValidation;

namespace Devices.Application.HouseholdDevices.SetAdminNotes;

internal sealed class SetAdminNotesValidator : AbstractValidator<SetAdminNotesCommand>
{
    public SetAdminNotesValidator() => RuleFor(c => c.DeviceId).NotEmpty();
}
