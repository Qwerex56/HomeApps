namespace Jarvis.Infrastructure.Gateway;

/// <summary>
/// JARVIS's own brain names its run in this header, so the gateway logs every tool call into it and holds destructive
/// ones for a confirmation. Honoured only with a delegated token of the run's person; other clients are not logged.
/// </summary>
public static class RunHeader
{
    public const string Name = "X-Jarvis-Run";
}
