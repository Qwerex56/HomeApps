using Devices.Application.Common;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.ListTools;

/// <summary>
/// The actions of my devices and my households' devices (S3) that are connected now, as MCP tools for JARVIS (<c>mac-biuro_say</c>). A device that is
/// offline or removed offers none, so JARVIS never sees a tool it cannot call.
/// </summary>
public sealed record ListToolsQuery : IQuery<IReadOnlyList<DeviceTool>>;
