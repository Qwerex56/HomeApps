using Households.Domain.Common;
using FluentResults;

namespace Households.Domain.Households;

/// <summary>
/// The household's JARVIS (household-jarvis-devices.md 2.1, D2, D9): one per household, created with it. Not a member:
/// not in the member list, not counted in the member limit, never an admin or owner. Satellites speak with its voice.
/// </summary>
public sealed class HouseholdAssistant
{
    public const string DefaultName = "JARVIS";
    public const int MaxNameLength = 32;
    public const int DefaultEventDays = 30;
    public const int MaxEventDays = 365;
    public const int DefaultLocationEventDays = 7;
    public const int MaxLocationEventDays = 90;

    private HouseholdAssistant() { }

    public bool Enabled { get; private set; } = true;

    public string Name { get; private set; } = DefaultName;

    public AssistantAccessLevel AccessLevel { get; private set; } = AssistantAccessLevel.ReadOnly;

    public AssistantToolSources ToolSources { get; private set; } = AssistantToolSources.All;

    /// <summary>D9: how long device events are kept.</summary>
    public int EventDays { get; private set; } = DefaultEventDays;

    /// <summary>D9: how long events with a location are kept; never longer than <see cref="EventDays"/>.</summary>
    public int LocationEventDays { get; private set; } = DefaultLocationEventDays;

    /// <summary>The role satellites get from the plugins' point of view.</summary>
    public HouseholdRole Role => AccessLevel == AssistantAccessLevel.Member ? HouseholdRole.Member : HouseholdRole.Guest;

    internal static HouseholdAssistant CreateDefault() => new();

    internal Result Update(
        bool enabled, string? name, AssistantAccessLevel accessLevel, AssistantToolSources toolSources, int eventDays, int locationEventDays)
    {
        var cleanName = name?.Trim() ?? string.Empty;
        if (cleanName.Length is 0 or > MaxNameLength || Text.HasControlCharacters(cleanName))
        {
            return Result.Fail(HouseholdErrors.AssistantNameInvalid);
        }

        if (!Enum.IsDefined(accessLevel) || (toolSources & ~AssistantToolSources.All) != 0)
        {
            return Result.Fail(HouseholdErrors.AssistantSettingsInvalid);
        }

        if (eventDays is < 1 or > MaxEventDays || locationEventDays < 1 || locationEventDays > Math.Min(MaxLocationEventDays, eventDays))
        {
            return Result.Fail(HouseholdErrors.AssistantRetentionInvalid);
        }

        Enabled = enabled;
        Name = cleanName;
        AccessLevel = accessLevel;
        ToolSources = toolSources;
        EventDays = eventDays;
        LocationEventDays = locationEventDays;
        return Result.Ok();
    }
}
