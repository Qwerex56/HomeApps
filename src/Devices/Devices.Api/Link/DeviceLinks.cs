using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using FluentResults;
using Microsoft.Extensions.Options;

namespace Devices.Api.Link;

/// <summary>The devices connected to this instance, one connection per device (a newer one replaces the older).</summary>
internal sealed class DeviceLinks(IOptions<DevicesOptions> options) : IDeviceLinks
{
    private readonly ConcurrentDictionary<Guid, DeviceConnection> _connected = new();

    public bool IsOnline(Guid deviceId) => _connected.ContainsKey(deviceId);

    public Task<Result<string>> RunAsync(Guid deviceId, string action, JsonElement? input, CancellationToken cancellationToken) =>
        RequestAsync(deviceId, connection => connection.RunAsync(action, input, options.Value.ActionTimeout, cancellationToken));

    public async Task<Result> SetAsync(Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken) =>
        (await RequestAsync(deviceId, connection => connection.SetAsync(control, value, options.Value.ActionTimeout, cancellationToken))).ToResult();

    private async Task<Result<string>> RequestAsync(Guid deviceId, Func<DeviceConnection, Task<Result<string>>> request)
    {
        if (!_connected.TryGetValue(deviceId, out var connection))
        {
            return Result.Fail(DeviceErrors.Offline);
        }

        try
        {
            return await request(connection);
        }
        catch (TimeoutException)
        {
            return Result.Fail(DeviceErrors.NoAnswer);
        }
        catch (WebSocketException)
        {
            return Result.Fail(DeviceErrors.Offline);
        }
    }

    public void Drop(Guid deviceId)
    {
        if (_connected.TryRemove(deviceId, out var connection))
        {
            connection.Fail();
            connection.Socket.Abort();
        }
    }

    public void Add(Guid deviceId, DeviceConnection connection)
    {
        if (_connected.TryGetValue(deviceId, out var older) && !ReferenceEquals(older, connection))
        {
            Drop(deviceId);
        }

        _connected[deviceId] = connection;
    }

    /// <summary>Removes the device only while this connection is still its current one.</summary>
    public void Remove(Guid deviceId, DeviceConnection connection)
    {
        if (_connected.TryRemove(new KeyValuePair<Guid, DeviceConnection>(deviceId, connection)))
        {
            connection.Fail();
        }
    }
}
