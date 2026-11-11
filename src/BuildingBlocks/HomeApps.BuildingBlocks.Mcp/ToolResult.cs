using System.Globalization;
using FluentResults;
using HomeApps.BuildingBlocks.Application;
using HomeApps.BuildingBlocks.Domain.Errors;
using ModelContextProtocol.Protocol;
using Result = FluentResults.Result;

namespace HomeApps.BuildingBlocks.Mcp;

/// <summary>
/// A tool answers in plain text that a model can repeat to a person (and JARVIS can read aloud), never JSON and
/// never Markdown. A failed <see cref="Result"/> becomes an error result with the same codes the HTTP API returns,
/// so the model can explain it instead of guessing.
/// </summary>
public static class ToolResult
{
    public static CallToolResult Text(string text) => new() { Content = [new TextContentBlock { Text = text }] };

    public static CallToolResult Error(string text) =>
        new() { Content = [new TextContentBlock { Text = text }], IsError = true };

    public static CallToolResult From<TValue>(Result<TValue> result, Func<TValue, string> describe)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(describe);
        return result.IsSuccess ? Text(describe(result.Value)) : Error(Describe(result.Errors));
    }

    public static CallToolResult From(Result result, string done)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? Text(done) : Error(Describe(result.Errors));
    }

    /// <summary>"Not allowed (household.forbidden): ..." per error; field errors name the field.</summary>
    public static string Describe(IEnumerable<IError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return string.Join(Environment.NewLine, errors.Select(Line));
    }

    private static string Line(IError error)
    {
        if (error.Metadata.TryGetValue(ValidationErrors.FieldKey, out var field))
        {
            return string.Create(CultureInfo.InvariantCulture, $"Invalid {field}: {error.Message}");
        }

        return error is AppError app
            ? string.Create(CultureInfo.InvariantCulture, $"{Title(app.Kind)} ({app.Code}): {app.Message}")
            : string.Create(CultureInfo.InvariantCulture, $"Failed: {error.Message}");
    }

    private static string Title(ErrorKind kind) => kind switch
    {
        ErrorKind.Unauthorized => "Not signed in",
        ErrorKind.Forbidden => "Not allowed",
        ErrorKind.NotFound => "Not found",
        ErrorKind.Conflict => "Conflict",
        ErrorKind.Locked => "Locked",
        _ => "Invalid",
    };
}
