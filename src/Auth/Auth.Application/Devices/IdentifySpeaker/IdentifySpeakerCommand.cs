using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Devices.IdentifySpeaker;

/// <summary>
/// jarvis-satellites.md S4: JARVIS heard a PIN on a household satellite and asks, on the internal network with his
/// service secret, which of the <paramref name="Candidates"/> (the household's people, narrowed by the name said)
/// it belongs to. Exactly one must match. Five misses in a row lock the satellite's PIN checks for 15 minutes.
/// </summary>
public sealed record IdentifySpeakerCommand(string ClientId, string ClientSecret, Guid DeviceId, IReadOnlyList<Guid> Candidates, string Pin)
    : ICommand<SpeakerView>;
