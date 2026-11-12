using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jarvis.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the model without starting the API. It never connects:
/// <c>dotnet ef migrations add &lt;Name&gt; -p src/Jarvis/Jarvis.Infrastructure -o Persistence/Migrations</c>.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<JarvisDbContext>
{
    public JarvisDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<JarvisDbContext>()
            .UseNpgsql("Host=localhost;Database=jarvis_design_time")
            .Options;
        return new JarvisDbContext(options, new NoPublisher());
    }
}
