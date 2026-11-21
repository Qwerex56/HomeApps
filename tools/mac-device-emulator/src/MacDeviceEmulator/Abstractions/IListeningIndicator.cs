namespace MacDeviceEmulator.Abstractions;

/// <summary>Maintains and displays the visible continuous listening status indicator.</summary>
public interface IListeningIndicator
{
    ListeningStatus CurrentStatus { get; }

    void SetStatus(ListeningStatus status, string? detail = null);
}
