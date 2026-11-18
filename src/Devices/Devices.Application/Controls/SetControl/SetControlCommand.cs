using System.Text.Json;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Controls.SetControl;

/// <summary>A household member changes a control of a household device (D6); the device answers, then reports its new state.</summary>
public sealed record SetControlCommand(Guid DeviceId, string? Control, JsonElement? Value) : ICommand;
