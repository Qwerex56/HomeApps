using System.Text.Json;

namespace Devices.Api.Link;

/// <summary>
/// A text frame on <c>/devices/link</c> (jarvis-satellites.md 2). The device sends <c>hello</c> once (with its <c>homeapps.device/1</c> descriptor, if it has one) and then
/// <c>action_result</c>s and <c>set_result</c>s, plus the <c>state</c>, <c>event</c> and <c>status</c> frames (<c>DeviceFrame</c>); the server sends <c>ready</c>, <c>action</c>s, <c>set</c>s and <c>error</c>.
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
    JsonElement? Descriptor = null,
    JsonElement? Value = null);
