using FluentResults;

namespace HomeApps.BuildingBlocks.Application;

/// <summary>
/// Input validation failures are plain FluentResults errors that name the field in their metadata. There are no
/// typed error classes yet: write one when a service really needs a different HTTP status.
/// </summary>
public static class ValidationErrors
{
    /// <summary>Metadata key holding the name of the invalid field.</summary>
    public const string FieldKey = "Field";

    public static Error ForField(string field, string message) => new Error(message).WithMetadata(FieldKey, field);
}
