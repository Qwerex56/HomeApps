namespace Jarvis.VoiceBridge;

/// <summary>What <c>POST /devices/pair</c> answers once: the secret is never shown again.</summary>
internal sealed record PairedDevice(Guid DeviceId, Guid HouseholdId, string Name, string Secret);
