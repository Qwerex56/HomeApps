namespace Jarvis.Application;

/// <summary>The assistant's own settings (section <c>Jarvis</c>, next to the gateway's).</summary>
public sealed class AssistantOptions
{
    public const string SectionName = "Jarvis";

    /// <summary>The global switch (JV10): off, every request gets a short "switched off" answer without the model.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>JV11: what all runs of a day (server's local day) may cost; <c>null</c> = no limit.</summary>
    public decimal? DailyBudgetUsd { get; set; }
}
