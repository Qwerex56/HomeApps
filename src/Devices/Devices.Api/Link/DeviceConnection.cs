using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Devices.Api.Link;

/// <summary>One connected device: sends it actions one frame at a time and matches its answers by id.</summary>
internal sealed class DeviceConnection(WebSocket socket) : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly SemaphoreSlim _sending = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<Result<string>>> _waiting = new(StringComparer.Ordinal);

    public WebSocket Socket => socket;

    public async Task SendAsync(LinkMessage message, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message, Json);
        await _sending.WaitAsync(cancellationToken);
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
        }
        finally
        {
            _sending.Release();
        }
    }

    public Task<Result<string>> RunAsync(string action, JsonElement? input, TimeSpan timeout, CancellationToken cancellationToken) =>
        RequestAsync(id => new LinkMessage("action", Id: id, Name: action, Input: input), timeout, cancellationToken);

    /// <summary>Sends <c>set</c> and waits for the device's <c>set_result</c>.</summary>
    public Task<Result<string>> SetAsync(string control, JsonElement value, TimeSpan timeout, CancellationToken cancellationToken) =>
        RequestAsync(id => new LinkMessage("set", Id: id, Name: control, Value: value), timeout, cancellationToken);

    private async Task<Result<string>> RequestAsync(Func<string, LinkMessage> frame, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        var answer = new TaskCompletionSource<Result<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting[id] = answer;
        try
        {
            await SendAsync(frame(id), cancellationToken);
            return await answer.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            _waiting.TryRemove(id, out _);
        }
    }

    /// <summary>The device's <c>action_result</c> or <c>set_result</c>; an unknown or late id is ignored.</summary>
    public void Answer(string id, bool ok, string? text)
    {
        if (_waiting.TryRemove(id, out var answer))
        {
            answer.TrySetResult(ok
                ? Result.Ok(text ?? string.Empty)
                : Result.Fail<string>(new AppError("devices.action_failed", text ?? "The device could not do it.", ErrorKind.Conflict)));
        }
    }

    /// <summary>The connection ended: everything still waiting fails.</summary>
    public void Fail()
    {
        foreach (var id in _waiting.Keys)
        {
            if (_waiting.TryRemove(id, out var answer))
            {
                answer.TrySetResult(Result.Fail<string>(DeviceErrors.Offline));
            }
        }
    }

    public void Dispose() => _sending.Dispose();
}
