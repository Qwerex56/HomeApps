using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Domain.Errors;
using MediatR;

namespace HomeApps.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Runs every FluentValidation validator for the request. On failure the handler is skipped and a
/// <see cref="ValidationError"/> is returned (no exception), which the API maps to a 400 ValidationProblem.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : ResultBase, new()
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var validatorList = validators as IValidator<TRequest>[] ?? validators.ToArray();
        if (validatorList.Length == 0)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validatorList.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

        if (failures.Count == 0)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var error = new ValidationError("validation.failed", "One or more validation errors occurred.", failures);
        return ResultFactory.Fail<TResponse>([error]);
    }
}
