using System.Text.Json;

namespace Devices.Api.Link;

/// <summary>
/// A text frame on <c>/devices/link</c> (jarvis-satellites.md 2). The device sends <c>hello</c> once (with its <c>homeapps.device/1</c> descriptor, if it has one) and then
/// <c>action_result</c>s; the server sends <c>ready</c>, <c>action</c>s and <c>error</c>.
/// </summary>
internal sealed record LinkMessage(
    string Type,
    string? Device = null,
    string? Kind = null,
    IReadOnlyList<LinkAction>? Actions = null,
    string? Id = null,
    string? Name = null,
    JsonElement? Input = null,
    bool? Ok = null,
    string? Text = null,
    Guid? DeviceId = null,
    string? Code = null,
    JsonElement? Descriptor = null);
