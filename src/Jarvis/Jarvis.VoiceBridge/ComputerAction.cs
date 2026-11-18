using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>
/// One thing JARVIS may do on this computer (jarvis-satellites.md 4), from the <c>--actions</c> file: what the server is
/// told (name, description, input schema, flags) and the shell command run here. Each input field reaches the command
/// as <c>JARVIS_&lt;FIELD&gt;</c>, the whole input as <c>JARVIS_INPUT</c>.
/// </summary>
internal sealed record ComputerAction(
    string Name,
    string Description,
    string Run,
    JsonElement? Input = null,
    bool ReadOnly = false,
    bool Confirm = true);
