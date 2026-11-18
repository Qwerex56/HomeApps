using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Devices.Domain.Devices;
using FluentValidation;

namespace Devices.Application.HouseholdDevices.AddHouseholdDevice;

internal sealed class AddHouseholdDeviceValidator : AbstractValidator<AddHouseholdDeviceCommand>
{
    public AddHouseholdDeviceValidator()
    {
        RuleFor(c => c.HouseholdId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(DeviceName.MaxLength);
    }
}
