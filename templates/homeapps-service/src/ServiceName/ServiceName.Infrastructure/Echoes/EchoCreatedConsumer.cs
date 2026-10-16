using HomeApps.Contracts.ServiceName;
using MassTransit;
using MediatR;
using ServiceName.Application.Echoes.MarkEchoDelivered;

namespace ServiceName.Infrastructure.Echoes;

/// <summary>
/// Sample consumer. Consumers stay thin: translate the message into a command and send it.
/// The inbox drops duplicates; the command's changes and anything it publishes commit together.
/// Consumers must be public: MassTransit's assembly scan only registers public types.
/// </summary>
public sealed class EchoCreatedConsumer(ISender sender) : IConsumer<EchoCreated>
{
    public async Task Consume(ConsumeContext<EchoCreated> context)
    {
        var result = await sender.Send(new MarkEchoDeliveredCommand(context.Message.EchoId), context.CancellationToken);
        if (result.IsFailed)
        {
            throw new InvalidOperationException($"Could not mark echo {context.Message.EchoId} delivered: {result.Errors[0].Message}");
        }
    }
}
