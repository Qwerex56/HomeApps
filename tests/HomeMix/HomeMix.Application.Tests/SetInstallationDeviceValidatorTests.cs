using System.Text.Json;
using HomeMix.Application.Devices.SetInstallationDevice;

namespace HomeMix.Application.Tests;

public sealed class SetInstallationDeviceValidatorTests
{
    private static readonly SetInstallationDeviceValidator Validator = new();

    [Fact]
    public void A_missing_value_is_refused()
    {
        var result = Validator.Validate(new SetInstallationDeviceCommand(Guid.NewGuid(), Guid.NewGuid(), "switch", default));

        Assert.Contains(result.Errors, e => e.ErrorMessage == "value is required");
    }

    [Fact]
    public void A_null_value_is_allowed()
    {
        using var json = JsonDocument.Parse("null");

        Assert.True(Validator.Validate(new SetInstallationDeviceCommand(Guid.NewGuid(), Guid.NewGuid(), "switch", json.RootElement)).IsValid);
    }
}
