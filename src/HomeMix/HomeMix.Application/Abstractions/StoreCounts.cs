namespace HomeMix.Application.Abstractions;

/// <summary>
/// The numbers of one add-on source (homemix-2.md 2.2): installations now (in households and on accounts), installs ever
/// and in the last 30 days, updates and removals. Never who.
/// </summary>
public sealed record StoreCounts(int Active, int Households, int Accounts, int Installs, int Installs30, int Updates, int Removals)
{
    public static StoreCounts None { get; } = new(0, 0, 0, 0, 0, 0, 0);
}
