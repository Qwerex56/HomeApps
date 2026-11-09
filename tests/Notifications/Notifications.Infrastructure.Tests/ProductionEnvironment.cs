using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Notifications.Infrastructure.Tests;

internal sealed class ProductionEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;

    public string ApplicationName { get; set; } = "Notifications.Api";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
