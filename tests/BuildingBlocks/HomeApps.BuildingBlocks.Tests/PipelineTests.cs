using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HomeApps.BuildingBlocks.Tests;

public sealed record CreateWidget(string Name) : ICommand<Guid>;

public sealed record DeleteWidget(string Name) : ICommand;

public sealed record GetWidget(string Name) : IQuery<string>;

public sealed class CreateWidgetValidator : AbstractValidator<CreateWidget>
{
    public CreateWidgetValidator() => RuleFor(c => c.Name).NotEmpty().MaximumLength(5);
}

public sealed class DeleteWidgetValidator : AbstractValidator<DeleteWidget>
{
    public DeleteWidgetValidator() => RuleFor(c => c.Name).NotEmpty();
}

public sealed class HandlerCalls
{
    public int Count { get; set; }
}

public sealed class CreateWidgetHandler(HandlerCalls calls) : ICommandHandler<CreateWidget, Guid>
{
    public Task<Result<Guid>> Handle(CreateWidget request, CancellationToken cancellationToken)
    {
        calls.Count++;
        return Task.FromResult(request.Name == "taken"
            ? Result.Fail<Guid>("Taken.")
            : Result.Ok(Guid.NewGuid()));
    }
}

public sealed class DeleteWidgetHandler : ICommandHandler<DeleteWidget>
{
    public Task<Result> Handle(DeleteWidget request, CancellationToken cancellationToken) => Task.FromResult(Result.Ok());
}

public sealed class GetWidgetHandler : IQueryHandler<GetWidget, string>
{
    public Task<Result<string>> Handle(GetWidget request, CancellationToken cancellationToken) => Task.FromResult(Result.Ok(request.Name));
}

public sealed class PipelineTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly HandlerCalls _calls = new();

    private ISender CreateSender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_unitOfWork);
        services.AddSingleton(_calls);
        services.AddHomeAppsApplication(typeof(PipelineTests).Assembly);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    [Fact]
    public async Task Invalid_command_returns_a_validation_error_and_skips_the_handler()
    {
        var result = await CreateSender().Send(new CreateWidget("too long name"));

        Assert.True(result.IsFailed);
        var error = result.Errors.Single();
        Assert.Equal("Name", error.Metadata[ValidationErrors.FieldKey]);
        Assert.Equal(0, _calls.Count);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Invalid_command_without_a_value_returns_a_validation_error()
    {
        var result = await CreateSender().Send(new DeleteWidget(""));

        Assert.Equal("Name", result.Errors.Single().Metadata[ValidationErrors.FieldKey]);
    }

    [Fact]
    public async Task Successful_command_saves_the_unit_of_work()
    {
        var result = await CreateSender().Send(new CreateWidget("ok"));

        Assert.True(result.IsSuccess);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_command_saves_nothing()
    {
        var result = await CreateSender().Send(new CreateWidget("taken"));

        Assert.Equal("Taken.", result.Errors.Single().Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Queries_never_save()
    {
        var result = await CreateSender().Send(new GetWidget("w"));

        Assert.Equal("w", result.Value);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
