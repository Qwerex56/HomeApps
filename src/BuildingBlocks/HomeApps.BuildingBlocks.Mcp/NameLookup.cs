using FluentResults;

namespace HomeApps.BuildingBlocks.Mcp;

/// <summary>
/// A model names things the way a person does ("Dom", "Ania", "Zakupy"), or by an id a tool gave it before. This finds
/// the one thing meant: an id matches exactly, a name ignoring case and surrounding spaces. Several matches are never
/// guessed: the error lists them, so the model can ask the person or retry with an id. Nothing named means the only
/// one there is ("my tasks" for someone in one household), and the list to choose from when there are more.
/// </summary>
public static class NameLookup
{
    private const int MaxListed = 10;

    /// <param name="what">What is being looked for, in the error messages ("household", "list", "person").</param>
    /// <param name="hint">Which tool lists them, e.g. "households_my_households".</param>
    public static Result<T> Find<T>(
        IReadOnlyCollection<T> items, string? input, Func<T, Guid> id, Func<T, string> name, string what, string? hint = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);

        var wanted = input?.Trim() ?? string.Empty;
        if (wanted.Length == 0)
        {
            return items.Count == 1 ? items.First() : Result.Fail<T>(items.Count == 0
                ? NotFound(what, wanted, items, name, hint)
                : LookupErrors.Ambiguous(what, $"Which {what}? {Known(items, name)}"));
        }

        if (Guid.TryParse(wanted, out var guid))
        {
            var byId = items.FirstOrDefault(i => id(i) == guid);
            return byId is not null ? byId : Result.Fail<T>(NotFound(what, wanted, items, name, hint));
        }

        var matches = items.Where(i => string.Equals(name(i).Trim(), wanted, StringComparison.OrdinalIgnoreCase)).ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => Result.Fail<T>(NotFound(what, wanted, items, name, hint)),
            _ => Result.Fail<T>(LookupErrors.Ambiguous(what,
                $"Several {what}s are called \"{wanted}\": {string.Join(", ", matches.Select(m => $"{name(m)} (id {id(m)})"))}. Use the id.")),
        };
    }

    private static Error NotFound<T>(string what, string wanted, IReadOnlyCollection<T> items, Func<T, string> name, string? hint)
    {
        var known = items.Count == 0 ? $"There is no {what} to choose from." : Known(items, name);
        var missing = wanted.Length == 0 ? string.Empty : $"No {what} \"{wanted}\". ";
        return LookupErrors.NotFound(what, $"{missing}{known}{(hint is null ? string.Empty : $" {hint} lists them.")}");
    }

    private static string Known<T>(IReadOnlyCollection<T> items, Func<T, string> name) =>
        $"Known: {string.Join(", ", items.Take(MaxListed).Select(name))}{(items.Count > MaxListed ? ", …" : string.Empty)}.";
}
