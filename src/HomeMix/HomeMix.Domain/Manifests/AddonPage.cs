namespace HomeMix.Domain.Manifests;

/// <summary>A page of the add-on: <c>/homes/{householdId}/mix/{id}/{Path}</c> or <c>/me/mix/{id}/{Path}</c>.</summary>
public sealed record AddonPage(string Path, LocalizedText Title, bool Menu);
