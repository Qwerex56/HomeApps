using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Auth.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the model without starting the API. It never connects:
/// <c>dotnet ef migrations add &lt;Name&gt; -p src/Auth/Auth.Infrastructure -o Persistence/Migrations</c>.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql("Host=localhost;Database=auth_design_time")
            .Options;
        return new AuthDbContext(options, new NoPublisher());
    }

    private sealed class NoPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
