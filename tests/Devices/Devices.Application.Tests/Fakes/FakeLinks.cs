using System.Text.Json;
using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using FluentResults;

namespace Devices.Application.Tests.Fakes;

internal sealed class FakeLinks : IDeviceLinks
{
    public bool Online { get; set; } = true;

    public List<(Guid DeviceId, string Control, string Value)> Sets { get; } = [];

    public bool IsOnline(Guid deviceId) => Online;

    public List<(Guid DeviceId, string Action)> Runs { get; } = [];

    public Task<Result<string>> RunAsync(Guid deviceId, string action, JsonElement? input, CancellationToken cancellationToken)
    {
        Runs.Add((deviceId, action));
        return Task.FromResult(Result.Ok("ok"));
    }

    public Task<Result> SetAsync(Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken)
    {
        if (!Online)
        {
            return Task.FromResult(Result.Fail(DeviceErrors.Offline));
        }

        Sets.Add((deviceId, control, value.GetRawText()));
        return Task.FromResult(Result.Ok());
    }

    public void Drop(Guid deviceId)
    {
    }
}
