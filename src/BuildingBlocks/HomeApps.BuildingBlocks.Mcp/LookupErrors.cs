using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeApps.BuildingBlocks.Mcp;

/// <summary><c>{what}.not_found</c> and <c>{what}.ambiguous</c> from <see cref="NameLookup"/>.</summary>
public static class LookupErrors
{
    public static Error NotFound(string what, string message) => new AppError($"{Code(what)}.not_found", message, ErrorKind.NotFound);

    public static Error Ambiguous(string what, string message) => new AppError($"{Code(what)}.ambiguous", message, ErrorKind.Conflict);

    private static string Code(string what) => what.Replace(' ', '_').ToLowerInvariant();
}
