using System.Text.Json;

namespace Devices.Api.Link;

/// <summary>An action in the device's <c>hello</c>: <paramref name="Input"/> is a JSON schema object, or absent.</summary>
internal sealed record LinkAction(string Name, string? Description, JsonElement? Input, bool ReadOnly, bool Confirm);
