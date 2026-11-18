using System.Runtime.CompilerServices;
using FluentResults;
using MediatR;

namespace Devices.Application.Tests.Fakes;

/// <summary>An <see cref="ISender"/> that records the requests sent through it and answers every one with success.</summary>
internal sealed class FakeSender : ISender
{
    public List<object> Sent { get; } = [];

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        Sent.Add(request);
        object ok = Result.Ok();
        return Task.FromResult((TResponse)ok);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        Sent.Add(request!);
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        Sent.Add(request);
        return Task.FromResult<object?>(Result.Ok());
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
