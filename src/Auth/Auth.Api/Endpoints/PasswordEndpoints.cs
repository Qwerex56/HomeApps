using Auth.Api.Http;
using Auth.Application.Password;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Auth.Api.Endpoints;

internal sealed record ForgotPasswordRequest(string Email);

internal sealed record ResetPasswordRequest(string Token, string Password);

/// <summary>A-08: forgotten password. Always 202, so nobody learns whether an address has an account (R9).</summary>
internal sealed class PasswordEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var password = app.MapGroup("/auth/password").WithTags("Password");

        password.MapPost("/forgot", async (
                ForgotPasswordRequest body, PerAddressLimiter perAddress, ISender sender, CancellationToken ct) =>
            {
                // Over the per-address limit the email is not sent, but the answer stays the same.
                if (string.IsNullOrWhiteSpace(body.Email) || !perAddress.TryAcquire(body.Email))
                {
                    return Results.Accepted();
                }

                return (await sender.Send(new ForgotPasswordCommand(body.Email), ct)).ToAcceptedResult();
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Mail);

        password.MapPost("/reset", async (ResetPasswordRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ResetPasswordCommand(body.Token, body.Password), ct)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);
    }
}
