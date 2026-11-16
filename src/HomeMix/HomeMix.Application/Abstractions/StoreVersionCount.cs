namespace HomeMix.Application.Abstractions;

/// <summary>How many active installations run a version.</summary>
public sealed record StoreVersionCount(string Version, int Installations);
