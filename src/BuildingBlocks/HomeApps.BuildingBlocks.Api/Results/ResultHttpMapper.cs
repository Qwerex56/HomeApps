using FluentResults;
using HomeApps.BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace HomeApps.BuildingBlocks.Api.Results;

/// <summary>
/// The one place where a <see cref="Result"/> becomes an HTTP response.
/// Success → 200/201/204. Failure → 400: a ValidationProblem when the errors name fields
/// (<see cref="ValidationErrors.FieldKey"/>), otherwise a ProblemDetails with the first error's message.
/// There are no typed errors yet; when a service needs 401/403/404/409, add the error type and map it here.
/// </summary>
public static class ResultHttpMapper
{
    public static IResult ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? HttpResults.NoContent() : ToProblem(result.Errors);
    }

    public static IResult ToHttpResult<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? HttpResults.Ok(result.Value) : ToProblem(result.Errors);
    }

    /// <summary>201 Created with a Location header built from the value, e.g. <c>id => $"/pings/{id}"</c>.</summary>
    public static IResult ToCreatedHttpResult<TValue>(this Result<TValue> result, Func<TValue, string> location)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(location);
        return result.IsSuccess ? HttpResults.Created(location(result.Value), result.Value) : ToProblem(result.Errors);
    }

    public static IResult ToProblem(IReadOnlyList<IError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("A failed result must carry at least one error.", nameof(errors));
        }

        var fieldErrors = errors.Where(e => e.Metadata.ContainsKey(ValidationErrors.FieldKey)).ToList();
        if (fieldErrors.Count > 0)
        {
            var failures = fieldErrors
                .GroupBy(e => e.Metadata[ValidationErrors.FieldKey]?.ToString() ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

            return TypedResults.ValidationProblem(failures, title: "Validation failed");
        }

        return HttpResults.Problem(
            detail: errors[0].Message,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Request could not be processed");
    }
}
