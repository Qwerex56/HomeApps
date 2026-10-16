using FluentResults;
using FluentValidation;
using MediatR;

namespace HomeApps.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Runs every FluentValidation validator for the request. On failure the handler is skipped and a failed result is
/// returned (no exception): one <see cref="Error"/> per failure, with the property name in
/// <see cref="ValidationErrors.FieldKey"/> metadata, which the API maps to a 400 ValidationProblem.
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

        var errors = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => ValidationErrors.ForField(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Count == 0)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        return ResultFactory.Fail<TResponse>(errors);
    }
}
