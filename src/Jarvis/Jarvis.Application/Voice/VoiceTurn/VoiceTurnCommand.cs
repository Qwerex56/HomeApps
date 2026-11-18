using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.Voice.VoiceTurn;

/// <summary>
/// One spoken turn from a listener (the Mac bridge, later a satellite): what was heard goes to the person's
/// conversation like a typed message, channel <c>voice</c>. Without a conversation a new one is started.
/// </summary>
/// <param name="Audio">16-bit little-endian mono PCM; empty when the listener heard it itself and sends <paramref name="Text"/>.</param>
/// <param name="Text">What was said, from a listener with its own speech engine (<c>speech=local</c>, S1).</param>
/// <param name="Pending">Actions JARVIS asked to confirm in the turn before: a short "tak" or "nie" decides them.</param>
/// <param name="Satellite">
/// The listener is a household satellite (its token names <paramref name="HouseholdId"/>): <paramref name="UserId"/> is
/// the device, and "tu Adrian, PIN 1234" makes the turn this person's (jarvis-satellites.md S4).
/// </param>
/// <param name="Speaker">The person the satellite's turn before was for, while JARVIS waits for them.</param>
public sealed record VoiceTurnCommand(
    Guid UserId,
    Guid? ConversationId,
    Guid? HouseholdId,
    ReadOnlyMemory<byte> Audio,
    int SampleRate,
    IReadOnlyList<Guid> Pending,
    string? Text = null,
    bool Satellite = false,
    Guid? Speaker = null) : ICommand<VoiceTurnView>;
