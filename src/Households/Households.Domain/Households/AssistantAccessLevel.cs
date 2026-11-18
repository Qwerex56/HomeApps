namespace Households.Domain.Households;

/// <summary>How much the household's assistant may do here: never more than a Member, never an admin.</summary>
public enum AssistantAccessLevel
{
    /// <summary>Like a Guest: only looks.</summary>
    ReadOnly = 0,

    /// <summary>Like a Member.</summary>
    Member = 1,
}
