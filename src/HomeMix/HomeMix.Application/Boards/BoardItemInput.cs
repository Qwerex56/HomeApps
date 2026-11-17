namespace HomeMix.Application.Boards;

/// <summary>One widget of a layout sent by the web app; <see cref="Fit"/> defaults to Center.</summary>
public sealed record BoardItemInput(string Key, int X, int Y, int W, int H, string? Fit);
