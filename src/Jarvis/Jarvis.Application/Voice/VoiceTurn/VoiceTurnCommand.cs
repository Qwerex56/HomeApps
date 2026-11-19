using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.Voice.VoiceTurn;

/// <summary>
/// One spoken turn from a listener (the Mac bridge, later a satellite): what was heard goes to the person's
/// conversation like a typed message, channel <c>voice</c>. Without a conversation a new one is started.
/// </summary>
/// <param name="Audio">16-bit little-endian mono PCM.</param>
/// <param name="Pending">Actions JARVIS asked to confirm in the turn before: a short "tak" or "nie" decides them.</param>
public sealed record VoiceTurnCommand(
    Guid UserId,
    Guid? ConversationId,
    Guid? HouseholdId,
    ReadOnlyMemory<byte> Audio,
    int SampleRate,
    IReadOnlyList<Guid> Pending) : ICommand<VoiceTurnView>;
