namespace HomeMix.Application.Abstractions;

/// <summary>Installs on one day (UTC).</summary>
public sealed record StoreDay(DateOnly Day, int Installs);
