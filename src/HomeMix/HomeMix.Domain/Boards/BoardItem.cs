namespace HomeMix.Domain.Boards;

/// <summary>
/// One widget on a board: its key (<c>core:household</c>, <c>mix:{installationId}</c> or <c>plugin:{pluginId}/{widgetId}</c>),
/// its column and row, width in columns (1 to 3), height in rows, and on the wall tablet whether it stays centred in
/// its standard size or fills its cell (<see cref="BoardFit"/>).
/// </summary>
public sealed record BoardItem(string Key, int X, int Y, int W, int H, string Fit);
