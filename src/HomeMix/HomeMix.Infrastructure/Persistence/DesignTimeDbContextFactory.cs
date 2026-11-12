using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeMix.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the model without starting the API. It never connects:
/// <c>dotnet ef migrations add &lt;Name&gt; -p src/HomeMix/HomeMix.Infrastructure -o Persistence/Migrations</c>.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HomeMixDbContext>
{
    public HomeMixDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HomeMixDbContext>()
            .UseNpgsql("Host=localhost;Database=homemix_design_time")
            .Options;
        return new HomeMixDbContext(options, new NoPublisher());
    }
}
