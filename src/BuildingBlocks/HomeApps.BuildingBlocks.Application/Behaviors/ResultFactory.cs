using FluentResults;

namespace HomeApps.BuildingBlocks.Application.Behaviors;

internal static class ResultFactory
{
    /// <summary>Builds a failed <see cref="Result"/> or <see cref="Result{TValue}"/> without knowing which one at compile time.</summary>
    public static TResponse Fail<TResponse>(IEnumerable<IError> errors)
        where TResponse : ResultBase, new()
    {
        var result = new TResponse();
        result.Reasons.AddRange(errors);
        return result;
    }
}
