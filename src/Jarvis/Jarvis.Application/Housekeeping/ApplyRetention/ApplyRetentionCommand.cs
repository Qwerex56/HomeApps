using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.Housekeeping.ApplyRetention;

/// <summary>JV12: texts of runs and calls go after 7 days, whole rows after 30. Run by a daily job.</summary>
public sealed record ApplyRetentionCommand : ICommand
{
    public static readonly TimeSpan ContentLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan RowLifetime = TimeSpan.FromDays(30);
}
