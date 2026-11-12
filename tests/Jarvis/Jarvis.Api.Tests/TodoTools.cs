using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Jarvis.Api.Tests;

[McpServerToolType]
internal sealed class TodoTools
{
    [McpServerTool(Name = "add_task", Destructive = false)]
    [Description("Adds a task.")]
    public static string AddTask(string title) => $"Added \"{title}\".";

    [McpServerTool(Name = "my_tasks", ReadOnly = true)]
    [Description("Lists the person's tasks.")]
    public static string MyTasks() => "Mleko";

    /// <summary>Not marked: MCP counts it destructive, so in JARVIS's run it waits for a confirmation.</summary>
    [McpServerTool(Name = "delete_task")]
    [Description("Deletes a task.")]
    public static string DeleteTask(string title) => $"Deleted \"{title}\".";
}
