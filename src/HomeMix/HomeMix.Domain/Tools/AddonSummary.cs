using HomeMix.Domain.Data;

namespace HomeMix.Domain.Tools;

/// <summary>A collection JARVIS reads as <c>{addonId}_summary</c> (read-only) to know what is going on in the add-on.</summary>
public sealed record AddonSummary(string Collection, DataScope Scope, string? Description);
