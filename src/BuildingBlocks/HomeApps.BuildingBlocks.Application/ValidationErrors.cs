using FluentResults;

namespace HomeApps.BuildingBlocks.Application;

/// <summary>
/// Input validation failures are plain FluentResults errors that name the field in their metadata.
/// Rule violations with another HTTP status use <c>AppError</c> (ADR 0008).
/// </summary>
public static class ValidationErrors
{
    /// <summary>Metadata key holding the name of the invalid field.</summary>
    public const string FieldKey = "Field";

    public static Error ForField(string field, string message) => new Error(message).WithMetadata(FieldKey, field);
}
