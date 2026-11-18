namespace Devices.Application.Common;

/// <summary>An action as the device announces it in <c>hello</c>; <paramref name="InputSchema"/> is a JSON schema object as text.</summary>
public sealed record ActionInput(string Name, string? Description, string? InputSchema, bool ReadOnly, bool Confirm);
