using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace ServiceName.Application.FeatureName.QueryName;

/// <summary>Read only: load, check the caller may see it, map to the response. Queries never save.</summary>
internal sealed class QueryNameQueryHandler : IQueryHandler<QueryNameQuery, QueryNameResponse>
{
    public Task<Result<QueryNameResponse>> Handle(QueryNameQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
