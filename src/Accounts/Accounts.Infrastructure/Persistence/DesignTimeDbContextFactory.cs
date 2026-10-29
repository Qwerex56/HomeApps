using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Accounts.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the model without starting the API. It never connects:
/// <c>dotnet ef migrations add &lt;Name&gt; -p src/Auth/Accounts.Infrastructure -o Persistence/Migrations</c>.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    public AccountsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseNpgsql("Host=localhost;Database=accounts_design_time")
            .Options;
        return new AccountsDbContext(options, new NoPublisher());
    }

    private sealed class NoPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
