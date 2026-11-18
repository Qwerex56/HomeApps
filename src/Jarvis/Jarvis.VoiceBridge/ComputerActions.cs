using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>Reads the <c>--actions</c> file: a JSON array of <see cref="ComputerAction"/>s (docs/devices.md).</summary>
internal static class ComputerActions
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { ReadCommentHandling = JsonCommentHandling.Skip };

    public static IReadOnlyList<ComputerAction> Load(string path)
    {
        var actions = JsonSerializer.Deserialize<List<ComputerAction>>(File.ReadAllText(path), Json) ?? [];
        if (actions.Any(a => string.IsNullOrWhiteSpace(a.Name) || string.IsNullOrWhiteSpace(a.Run)))
        {
            throw new InvalidOperationException($"{path}: każda akcja potrzebuje „name” i „run”.");
        }

        return actions;
    }
}
