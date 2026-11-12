namespace HomeMix.Application.Data;

/// <summary>How long an uninstalled add-on's data waits: reinstalling within this time brings it back, after it the data is deleted.</summary>
public static class DataRetention
{
    public static readonly TimeSpan AfterRemoval = TimeSpan.FromDays(30);
}
