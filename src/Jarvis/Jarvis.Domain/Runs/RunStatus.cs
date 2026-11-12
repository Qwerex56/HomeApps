namespace Jarvis.Domain.Runs;

public enum RunStatus
{
    Running = 0,
    Finished = 1,
    Failed = 2,

    /// <summary>Not started: JARVIS is switched off, or the day's budget is spent (JV10, JV11).</summary>
    Refused = 3,
}
