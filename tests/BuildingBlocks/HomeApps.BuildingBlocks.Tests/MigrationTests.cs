using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace HomeApps.BuildingBlocks.Tests;

public sealed class MigrationTests
{
    [Theory]
    [InlineData("Development", null, true)]
    [InlineData("Production", null, false)]
    [InlineData("Production", "true", true)]
    [InlineData("Development", "false", false)]
    public void Migrates_on_start_in_development_or_when_the_setting_says_so(string environmentName, string? setting, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [MigrationExtensions.MigrateOnStartupKey] = setting })
            .Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        Assert.Equal(expected, MigrationExtensions.ShouldMigrateOnStartup(configuration, environment));
    }
}
