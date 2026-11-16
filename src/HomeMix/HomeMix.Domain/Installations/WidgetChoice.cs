namespace HomeMix.Domain.Installations;

/// <summary>
/// What a household's admins (or an account's owner) chose for one place: which widget of the add-on stands there,
/// in which size and where among the others; <see cref="WidgetId"/> <c>null</c> turns the add-on's widgets off there.
/// Without a choice every widget that lists the place stands there in its default size.
/// </summary>
public sealed record WidgetChoice(string Place, string? WidgetId, string? Size, int Order);
