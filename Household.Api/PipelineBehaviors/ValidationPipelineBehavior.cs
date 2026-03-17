using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;


namespace Household.Api.PipelineBehaviors;

public class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result {

    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators) {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken ct) {
        if (!_validators.Any()) {
            return await next(ct);
        }

        var errors = _validators
            .Select(validator => validator.Validate(request))
            .SelectMany(result => result.Errors)
            .Where(validationFailure => validationFailure is not null)
            .Select(failure => new Error(
                failure.PropertyName))
            .Distinct()
            .ToArray();

        if (errors.Length != 0) {
            return (Result.Fail(errors) as TResponse)!;
        }
        
        return await next(ct);
    }
}