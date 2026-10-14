using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace HomeApps.BuildingBlocks.Api.Results;

/// <summary>
/// The one place where a <see cref="Result"/> becomes an HTTP response.
/// Success → 200/201/204. Failure → ProblemDetails whose status comes from the first typed error:
/// <see cref="ValidationError"/> 400, <see cref="UnauthorizedError"/> 401, <see cref="ForbiddenError"/> 403,
/// <see cref="NotFoundError"/> 404, <see cref="ConflictError"/> 409, any other error 422.
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

        var first = errors[0];
        var code = first is DomainError domainError ? domainError.Code : "error";

        if (first is ValidationError)
        {
            var failures = errors
                .OfType<ValidationError>()
                .SelectMany(e => e.Failures.Count > 0 ? e.Failures : new Dictionary<string, string[]> { [string.Empty] = [e.Message] })
                .GroupBy(kv => kv.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.SelectMany(kv => kv.Value).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

            return TypedResults.ValidationProblem(
                failures,
                detail: first.Message,
                title: "Validation failed",
                extensions: new Dictionary<string, object?> { ["code"] = code });
        }

        var (status, title) = first switch
        {
            UnauthorizedError => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenError => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundError => (StatusCodes.Status404NotFound, "Not found"),
            ConflictError => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status422UnprocessableEntity, "Request could not be processed"),
        };

        return HttpResults.Problem(
            detail: first.Message,
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}
