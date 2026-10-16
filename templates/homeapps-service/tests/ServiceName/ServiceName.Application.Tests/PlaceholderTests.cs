using ServiceName.Application;

namespace ServiceName.Application.Tests;

// Delete this once the first handler has real tests.
public sealed class PlaceholderTests
{
    [Fact]
    public void Application_assembly_loads() => Assert.NotNull(typeof(DependencyInjection).Assembly);
}
