using Devices.Application.Common;
using Devices.Application.Tools.ListTools;
using FluentResults;
using MediatR;

namespace Devices.Api.Tests;

/// <summary>An <see cref="ISender"/> that answers <see cref="ListToolsQuery"/> with the tools it was given and nothing else.</summary>
internal sealed class FakeToolSender(params DeviceTool[] tools) : ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is not ListToolsQuery)
        {
            throw new NotSupportedException();
        }

        object result = Result.Ok<IReadOnlyList<DeviceTool>>(tools);
        return Task.FromResult((TResponse)result);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest => throw new NotSupportedException();

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
