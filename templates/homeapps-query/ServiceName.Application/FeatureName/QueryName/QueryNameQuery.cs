using HomeApps.BuildingBlocks.Application.Messaging;

namespace ServiceName.Application.FeatureName.QueryName;

public sealed record QueryNameQuery() : IQuery<QueryNameResponse>;

public sealed record QueryNameResponse();
