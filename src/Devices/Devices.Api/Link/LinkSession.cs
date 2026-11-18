using System.Net.WebSockets;
using System.Text.Json;
using Devices.Application.Common;
using Devices.Application.Links.ConnectDevice;
using Devices.Application.Links.DisconnectDevice;
using Devices.Domain.Devices;
using HomeApps.BuildingBlocks.Domain.Errors;
using MediatR;

namespace Devices.Api.Link;

/// <summary>
/// One device's connection: the first frame must be <c>hello</c> (who it is, what it can do), answered by <c>ready</c>
/// with the device's id or by <c>error</c> and a close; after that the device only answers actions.
/// </summary>
internal sealed partial class LinkSession(WebSocket socket, DeviceLinks links, IServiceScopeFactory scopes, ILogger<LinkSession> logger)
{
    private static readonly TimeSpan HelloTimeout = TimeSpan.FromSeconds(10);
    private const int MaxFrame = 256 * 1024;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var connection = new DeviceConnection(socket);
        Guid? deviceId = null;
        try
        {
            using var helloTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            helloTimeout.CancelAfter(HelloTimeout);
            var hello = await ReceiveAsync(helloTimeout.Token);
            if (hello?.Type != "hello")
            {
                await RefuseAsync(connection, "devices.hello_expected", "The first message must be hello.", cancellationToken);
                return;
            }

            deviceId = await ConnectAsync(connection, hello, cancellationToken);
            if (deviceId is null)
            {
                return;
            }

            links.Add(deviceId.Value, connection);
            await connection.SendAsync(new LinkMessage("ready", DeviceId: deviceId), cancellationToken);
            LogConnected(deviceId.Value, hello.Device);

            while (await ReceiveAsync(cancellationToken) is { } message)
            {
                if (message.Type == "action_result" && message.Id is { } id)
                {
                    connection.Answer(id, message.Ok == true, message.Text);
                }
            }
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or JsonException)
        {
            // The device went away or sent something that is not JSON.
        }
        finally
        {
            if (deviceId is { } id)
            {
                links.Remove(id, connection);
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISender>().Send(new DisconnectDeviceCommand(id), CancellationToken.None);
            }
        }
    }

    private async Task<Guid?> ConnectAsync(DeviceConnection connection, LinkMessage hello, CancellationToken cancellationToken)
    {
        var kind = string.Equals(hello.Kind, "satellite", StringComparison.OrdinalIgnoreCase) ? DeviceKind.Satellite : DeviceKind.Computer;
        var actions = (hello.Actions ?? [])
            .Select(a => new ActionInput(a.Name, a.Description, a.Input is { ValueKind: JsonValueKind.Object } input ? input.GetRawText() : null, a.ReadOnly, a.Confirm))
            .ToList();

        await using var scope = scopes.CreateAsyncScope();
        var connected = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new ConnectDeviceCommand(hello.Device ?? string.Empty, kind, actions), cancellationToken);
        if (connected.IsSuccess)
        {
            return connected.Value;
        }

        var error = connected.Errors[0];
        await RefuseAsync(connection, error is AppError app ? app.Code : "devices.invalid", error.Message, cancellationToken);
        return null;
    }

    private async Task RefuseAsync(DeviceConnection connection, string code, string message, CancellationToken cancellationToken)
    {
        await connection.SendAsync(new LinkMessage("error", Code: code, Text: message), cancellationToken);
        await socket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, code, cancellationToken);
    }

    /// <summary>The next text frame, or <c>null</c> when the device closed the connection.</summary>
    private async Task<LinkMessage?> ReceiveAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var frame = new MemoryStream();
        while (true)
        {
            var received = await socket.ReceiveAsync(buffer.AsMemory(), cancellationToken);
            if (received.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                return null;
            }

            frame.Write(buffer, 0, received.Count);
            if (frame.Length > MaxFrame)
            {
                throw new WebSocketException("The frame is too large.");
            }

            if (!received.EndOfMessage)
            {
                continue;
            }

            if (received.MessageType == WebSocketMessageType.Text)
            {
                return JsonSerializer.Deserialize<LinkMessage>(frame.ToArray(), DeviceConnection.Json);
            }

            frame.SetLength(0);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Device {DeviceId} ({Name}) connected")]
    private partial void LogConnected(Guid deviceId, string? name);
}
