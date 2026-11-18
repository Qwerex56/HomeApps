using System.Text.Json;

namespace Jarvis.VoiceBridge.Tests;

/// <summary>The <c>--actions</c> file and running its commands (jarvis-satellites.md 4, S9–S11).</summary>
public sealed class ComputerActionsTests : IDisposable
{
    private readonly string _file = Path.GetTempFileName();

    [Fact]
    public void The_file_lists_actions_and_changes_are_confirmed_by_default()
    {
        File.WriteAllText(_file, """
            [
              // comments are allowed
              { "name": "say", "description": "Mówi", "run": "say \"$JARVIS_TEXT\"",
                "input": { "type": "object", "properties": { "text": { "type": "string" } } } },
              { "name": "battery", "description": "Bateria", "run": "pmset -g batt", "readOnly": true }
            ]
            """);

        var actions = ComputerActions.Load(_file);

        Assert.Equal(["say", "battery"], actions.Select(a => a.Name));
        Assert.True(actions[0].Confirm);
        Assert.True(actions[1].ReadOnly);
        Assert.Equal(JsonValueKind.Object, actions[0].Input!.Value.ValueKind);
    }

    [Fact]
    public void An_action_without_a_command_is_refused()
    {
        File.WriteAllText(_file, """[{ "name": "say", "description": "Mówi" }]""");

        Assert.Throws<InvalidOperationException>(() => ComputerActions.Load(_file));
    }

    [Fact]
    public async Task Input_reaches_the_command_as_environment_and_never_as_shell_text()
    {
        var action = new ComputerAction("echo", "Echo", "printf '%s|%s' \"$JARVIS_TEXT\" \"$JARVIS_COUNT\"");
        var input = JsonSerializer.SerializeToElement(new { text = "$(rm -rf ~); ok", count = 3 });

        var (ok, text) = await CommandRunner.RunAsync(action, input, CancellationToken.None);

        Assert.True(ok);
        Assert.Equal("$(rm -rf ~); ok|3", text);
    }

    [Fact]
    public async Task A_failing_command_reports_its_exit_code()
    {
        var (ok, text) = await CommandRunner.RunAsync(new ComputerAction("fail", "x", "echo nope >&2; exit 3"), null, CancellationToken.None);

        Assert.False(ok);
        Assert.Equal("Kod wyjścia 3. nope", text);
    }

    public void Dispose() => File.Delete(_file);
}
