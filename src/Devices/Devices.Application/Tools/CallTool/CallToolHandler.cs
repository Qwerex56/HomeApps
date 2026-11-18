using System.Text.Json;
using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Application.Controls.SetControl;
using Devices.Application.Telemetry.GetDeviceEvents;
using Devices.Application.Telemetry.GetDeviceState;
using Devices.Application.Tools.DescribeDevices;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using MediatR;

namespace Devices.Application.Tools.CallTool;

internal sealed class CallToolHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceLinks links, ICurrentUser currentUser, ISender sender)
    : ICommandHandler<CallToolCommand, string>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<string>> Handle(CallToolCommand command, CancellationToken cancellationToken)
    {
        if (command.Tool == GeneratedTools.Describe)
        {
            return await DescribeAsync(command.Input, cancellationToken);
        }

        // Device slugs have no '_', so the first one splits "mac-biuro_say_hello" into the device and its action.
        var split = command.Tool.IndexOf('_', StringComparison.Ordinal);
        if (split <= 0)
        {
            return Result.Fail(DeviceErrors.ActionNotFound);
        }

        // The slug is looked up among the caller's reachable devices, each with its own unique tool slug (one device per name, never "the first").
        var name = command.Tool[..split];
        var reachable = await ReachableDevices.OfCallerAsync(devices, households, currentUser.RequiredUserId, cancellationToken);
        var found = reachable.FirstOrDefault(r => r.Slug == name);
        if (found is null)
        {
            return Result.Fail(DeviceErrors.NotFound);
        }

        var device = found.Device;
        var actionName = command.Tool[(split + 1)..];
        if (GeneratedTools.Applies(device) && GeneratedTools.IsReserved(actionName))
        {
            return await GeneratedAsync(device, actionName, command.Input, cancellationToken);
        }

        var action = device.Find(actionName);
        if (action is null)
        {
            return Result.Fail(DeviceErrors.ActionNotFound);
        }

        return await links.RunAsync(device.Id, action.Name, command.Input, cancellationToken);
    }

    private async Task<Result<string>> DescribeAsync(JsonElement? input, CancellationToken cancellationToken)
    {
        if (input is not { ValueKind: JsonValueKind.Object } o || !o.TryGetProperty("household", out var id) || id.ValueKind != JsonValueKind.String || !Guid.TryParse(id.GetString(), out var household))
        {
            return Result.Fail(DeviceErrors.HouseholdRequired);
        }

        var described = await sender.Send(new DescribeDevicesQuery(household), cancellationToken);
        return described.IsFailed ? described.ToResult() : Serialize(described.Value);
    }

    private async Task<Result<string>> GeneratedAsync(Device device, string action, JsonElement? input, CancellationToken cancellationToken)
    {
        switch (action)
        {
            case GeneratedTools.State:
                var state = await sender.Send(new GetDeviceStateQuery(device.Id), cancellationToken);
                return state.IsFailed ? state.ToResult() : Serialize(state.Value);
            case GeneratedTools.Events:
                var events = await sender.Send(new GetDeviceEventsQuery(device.Id, 0, GeneratedTools.EventLimit(input)), cancellationToken);
                return events.IsFailed ? events.ToResult() : Serialize(events.Value);
            default:
                // The same use case as REST's POST /devices/{id}/set: the same D6 rights, validation against the descriptor and the link.
                string? control = input is { ValueKind: JsonValueKind.Object } o && o.TryGetProperty("control", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                JsonElement? value = input is { ValueKind: JsonValueKind.Object } v && v.TryGetProperty("value", out var given) ? given.Clone() : null;
                var set = await sender.Send(new SetControlCommand(device.Id, control, value), cancellationToken);
                return set.IsFailed ? Result.Fail<string>(set.Errors) : Result.Ok("Done.");
        }
    }

    private static Result<string> Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);
}
