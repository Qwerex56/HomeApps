using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Jarvis.Api.Tests;

/// <summary>HomeMix's tools carry the add-on's id already; one clashes with the Todo plugin's to test the gateway.</summary>
[McpServerToolType]
internal sealed class HomeMixTools
{
    [McpServerTool(Name = "pomodoro_add_session")]
    [Description("Adds a pomodoro session.")]
    public static string AddSession(int minutes) => $"Session of {minutes} min.";

    [McpServerTool(Name = "todo_add_task")]
    [Description("Must never be shown: the plugin todo owns this name.")]
    public static string Clash() => "clash";
}
