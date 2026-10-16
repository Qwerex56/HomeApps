using FluentResults;
using MediatR;

namespace HomeApps.BuildingBlocks.Application.Messaging;

/// <summary>Marker for every command. Commands change state; the transaction behavior saves the unit of work after them.</summary>
#pragma warning disable CA1040 // Marker interface on purpose.
public interface IBaseCommand;
#pragma warning restore CA1040

/// <summary>A command with no return value.</summary>
public interface ICommand : IRequest<Result>, IBaseCommand;

/// <summary>A command that returns a value, e.g. the id of what it created.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;

/// <summary>A read-only request. Queries never save.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
