using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace ServiceName.Application.FeatureName.CommandName;

/// <summary>
/// Load the aggregate through its repository, call one domain method and return its Result.
/// Don't save: the transaction behavior commits after a successful result.
/// </summary>
#if (hasReturn)
internal sealed class CommandNameCommandHandler : ICommandHandler<CommandNameCommand, TReturn>
{
    public Task<Result<TReturn>> Handle(CommandNameCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
#else
internal sealed class CommandNameCommandHandler : ICommandHandler<CommandNameCommand>
{
    public Task<Result> Handle(CommandNameCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
#endif
