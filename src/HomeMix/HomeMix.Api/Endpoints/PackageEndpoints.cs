using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Api.Requests;
using HomeMix.Application.Packages;
using HomeMix.Application.Packages.AddPackageFromGitHub;
using HomeMix.Application.Packages.AddPackageFromZip;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary>
/// Step one of installing: add a package from a zip or a GitHub repository and get back what it asks for, for the
/// consent screen. Nothing is installed yet (docs/homemix.md, "Where packages come from").
/// </summary>
internal sealed class PackageEndpoints : IEndpoint
{
    /// <summary>The zip limit is configuration (5 MB by default); this only stops a huge body before it is read.</summary>
    private const long MaxRequestBytes = 52 * 1024 * 1024;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var packages = app.MapGroup("/homemix/api/packages").WithTags("Packages");

        packages.MapPost("/", async (IFormFile file, ISender sender, CancellationToken ct) =>
            {
                await using var stream = file.OpenReadStream();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, ct);
                return (await sender.Send(new AddPackageFromZipCommand(buffer.ToArray()), ct)).ToHttpResult();
            })
            .DisableAntiforgery()
            .WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(MaxRequestBytes))
            .Produces<PackageView>();

        packages.MapPost("/github", async (GitHubPackageRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddPackageFromGitHubCommand(body.Repository, body.Tag), ct)).ToHttpResult())
            .Produces<PackageView>();
    }
}
