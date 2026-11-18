namespace Auth.Application.Devices;

/// <summary>
/// The person whose PIN was heard, or <c>null</c> when nobody matched (a success, so the miss is counted and saved).
/// JARVIS then asks for a delegated token for them as usual.
/// </summary>
public sealed record SpeakerView(Guid? UserId);
