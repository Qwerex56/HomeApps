using FluentValidation;

namespace Devices.Application.Telemetry.GetDeviceEvents;

internal sealed class GetDeviceEventsValidator : AbstractValidator<GetDeviceEventsQuery>
{
    public GetDeviceEventsValidator()
    {
        RuleFor(q => q.Skip).GreaterThanOrEqualTo(0);
        RuleFor(q => q.Limit).InclusiveBetween(1, GetDeviceEventsQuery.MaxLimit);
    }
}
