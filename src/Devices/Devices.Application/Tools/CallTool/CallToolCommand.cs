using System.Text.Json;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.CallTool;

/// <summary>
/// JARVIS calls an action of my connected device by its tool name (<c>mac-biuro_say</c>); the device runs it and its
/// plain-text answer comes back. Confirmation ("tak/nie") already happened in JARVIS's gateway when the action needed it.
/// </summary>
public sealed record CallToolCommand(string Tool, JsonElement? Input) : ICommand<string>;
