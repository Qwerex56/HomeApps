using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Api.Sandbox;
using HomeMix.Application.Run.GetRunFile;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary>
/// An installed add-on's files for its sandboxed frame, anonymously (the installation id is a UUID, files are only
/// code; data needs a token). Removed or blocked installations answer 404.
/// </summary>
internal sealed class RunEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var run = app.MapGroup("/homemix/run").WithTags("Sandbox").AllowAnonymous();

        run.MapGet("/{installationId:guid}/{**path}", async (Guid installationId, string? path, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            // Relative links in the add-on's pages need the trailing slash.
            if (string.IsNullOrEmpty(path) && !http.Request.Path.Value!.EndsWith('/'))
            {
                return Results.Redirect($"/homemix/run/{installationId}/");
            }

            var file = await sender.Send(new GetRunFileQuery(installationId, path ?? string.Empty), ct);
            if (file.IsFailed)
            {
                return file.ToHttpResult();
            }

            SandboxHeaders.Apply(http.Response.Headers, file.Value.Network);
            return Results.Stream(file.Value.Content, ContentTypes.For(file.Value.Path));
        });
    }
}
