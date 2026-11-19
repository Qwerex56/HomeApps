using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Triggers;

/// <summary>A trigger of an add-on installed in a household, with how it is doing (the admin's list, UI in H-9).</summary>
public sealed record TriggerView(
    Guid InstallationId,
    string AddonId,
    LocalizedText AddonName,
    string Id,
    string On,
    string? Capability,
    string? Role,
    string? Model,
    string? Field,
    string? Name,
    bool Enabled,
    int ConsecutiveFailures,
    DateTimeOffset? LastRunAt,
    string? LastError,
    bool DisabledByFailures);
