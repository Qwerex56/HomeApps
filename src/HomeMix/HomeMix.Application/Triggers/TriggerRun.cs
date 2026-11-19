using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Triggers;

/// <summary>One trigger of one installation to run for an occurrence.</summary>
public sealed record TriggerRun(Installation Installation, AddonManifest Manifest, AddonTrigger Trigger, TriggerOccurrence Occurrence);
