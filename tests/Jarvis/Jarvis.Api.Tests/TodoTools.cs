using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Jarvis.Api.Tests;

[McpServerToolType]
internal sealed class TodoTools
{
    [McpServerTool(Name = "add_task", Destructive = false)]
    [Description("Adds a task.")]
    public static string AddTask(string title) => $"Added \"{title}\".";
}
