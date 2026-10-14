using HomeApps.BuildingBlocks.Application.Messaging;

namespace ServiceName.Application.FeatureName.CommandName;

#if (hasReturn)
public sealed record CommandNameCommand() : ICommand<TReturn>;
#else
public sealed record CommandNameCommand() : ICommand;
#endif
