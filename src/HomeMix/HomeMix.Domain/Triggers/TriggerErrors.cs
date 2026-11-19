using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeMix.Domain.Triggers;

public static class TriggerErrors
{
    public static Error NotFound => new AppError("homemix.trigger_not_found", "This add-on declares no such trigger.", ErrorKind.NotFound);

    public static Error ControlMissing => new AppError(
        "homemix.capability_missing", "The add-on did not declare devices.control, so its trigger may only read devices.", ErrorKind.Forbidden);

    public static Error NoServiceClient => new AppError(
        "homemix.service_client_missing", "HomeMix has no service client secret (HomeMix:ClientSecret), so triggers cannot reach Devices.", ErrorKind.Conflict);
}
