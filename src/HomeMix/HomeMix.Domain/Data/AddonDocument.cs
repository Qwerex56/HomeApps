using FluentResults;

namespace HomeMix.Domain.Data;

/// <summary>
/// One JSON document in an installation's space, under a collection the add-on names and a key. <see cref="Version"/>
/// guards against lost updates: a write that names an older version is refused.
/// </summary>
public sealed class AddonDocument
{
    private AddonDocument() { }

    public Guid Id { get; private set; }

    public Guid InstallationId { get; private set; }

    public DataScope Scope { get; private set; }

    /// <summary>The person whose private document it is (<see cref="DataScope.User"/>), otherwise <c>null</c>.</summary>
    public Guid? OwnerUserId { get; private set; }

    public string Collection { get; private set; } = string.Empty;

    public string Key { get; private set; } = string.Empty;

    /// <summary>The JSON text.</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>UTF-8 bytes of <see cref="Value"/>, counted against the installation's limit.</summary>
    public int Size { get; private set; }

    public int Version { get; private set; }

    public Guid UpdatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static AddonDocument Create(
        Guid installationId, DataScope scope, Guid? ownerUserId, string collection, string key, string value, int size, Guid by,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            InstallationId = installationId,
            Scope = scope,
            OwnerUserId = scope == DataScope.User ? ownerUserId : null,
            Collection = collection,
            Key = key,
            Value = value,
            Size = size,
            Version = 1,
            UpdatedBy = by,
            UpdatedAt = now,
        };

    /// <param name="expectedVersion">The version the writer read, or <c>null</c> to overwrite whatever is there.</param>
    public Result Replace(string value, int size, int? expectedVersion, Guid by, DateTimeOffset now)
    {
        if (!Matches(expectedVersion))
        {
            return Result.Fail(DataErrors.Conflict);
        }

        Value = value;
        Size = size;
        Version++;
        UpdatedBy = by;
        UpdatedAt = now;
        return Result.Ok();
    }

    /// <summary>A reinstall of the same add-on in the same place takes over the data of the removed installation.</summary>
    public void MoveTo(Guid installationId) => InstallationId = installationId;

    public bool Matches(int? expectedVersion) => expectedVersion is null || expectedVersion == Version;
}
