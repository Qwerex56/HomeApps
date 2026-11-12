using Jarvis.Application.Abstractions;
using Jarvis.Infrastructure.Brain;

namespace Jarvis.Api.Tests;

public sealed class StreamJsonReaderTests
{
    [Fact]
    public void Text_comes_in_pieces_and_the_result_line_finishes_with_model_and_cost()
    {
        var reader = new StreamJsonReader();
        string[] lines =
        [
            """{"type":"system","subtype":"init","model":"claude-x"}""",
            """{"type":"assistant","message":{"model":"claude-x","content":[{"type":"text","text":"Dodałem "},{"type":"tool_use","name":"mcp__homeapps__todo_add_task"}]}}""",
            """{"type":"assistant","message":{"content":[{"type":"text","text":"mleko."}]}}""",
            """{"type":"result","subtype":"success","result":"Dodałem mleko.","total_cost_usd":0.012}""",
            "not json",
        ];

        var events = lines.SelectMany(reader.Read).ToList();

        Assert.Equal(["Dodałem ", "mleko."], events.OfType<BrainText>().Select(e => e.Text));
        var finished = Assert.Single(events.OfType<BrainFinished>());
        Assert.Equal("Dodałem mleko.", finished.Text);
        Assert.Equal("claude-x", finished.Model);
        Assert.Equal(0.012m, finished.CostUsd);
    }
}
