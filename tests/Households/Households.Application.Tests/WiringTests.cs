using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Households.Application.Tests;

public sealed class WiringTests
{
    [Fact]
    public void EveryHandlerResolves()
    {
        using var w = new World();
        using var scope = w.Services.CreateScope();

        var handlerTypes = typeof(DependencyInjection).Assembly.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                || i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .Distinct()
            .ToList();

        Assert.NotEmpty(handlerTypes);
        Assert.All(handlerTypes, t => Assert.NotEmpty(scope.ServiceProvider.GetServices(t)));
    }
}
