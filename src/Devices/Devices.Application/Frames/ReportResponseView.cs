using System.Text.Json;

namespace Devices.Application.Frames;

/// <summary>The answer to <c>POST /devices/report</c>: one result per frame, and the settings the device has not applied yet (null when none).</summary>
public sealed record ReportResponseView(IReadOnlyList<FrameResult> Results, JsonElement? Settings);
