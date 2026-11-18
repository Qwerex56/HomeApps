namespace Devices.Application.Frames;

/// <summary>What became of one frame: accepted, or refused with a code and the reason. One bad frame does not stop the others.</summary>
public sealed record FrameResult(bool Ok, string? Code = null, string? Text = null);
