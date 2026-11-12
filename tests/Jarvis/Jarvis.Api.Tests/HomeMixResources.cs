using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Jarvis.Api.Tests;

[McpServerResourceType]
internal sealed class HomeMixResources
{
    [McpServerResource(UriTemplate = "homemix://pomodoro/summary", Name = "pomodoro summary", MimeType = "text/plain")]
    [Description("Today's sessions.")]
    public static string Summary() => "3 sessions today";
}
