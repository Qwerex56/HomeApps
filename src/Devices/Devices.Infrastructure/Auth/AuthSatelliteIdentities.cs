using System.Net.Http.Json;
using Devices.Application.Abstractions;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using Microsoft.Extensions.Configuration;

namespace Devices.Infrastructure.Auth;

/// <summary>
/// Auth's <c>POST /internal/devices</c> on the Docker network, with Devices' service client (<c>Devices:ClientId</c>,
/// <c>Devices:ClientSecret</c>). Anything but success is "pairing unavailable": the code stays usable.
/// </summary>
internal sealed class AuthSatelliteIdentities(HttpClient http, IConfiguration configuration) : ISatelliteIdentities
{
    private static readonly AppError Unavailable = new(
        "devices.pairing_unavailable", "Pairing is not available right now; try the same code again in a moment.", ErrorKind.Conflict);

    public async Task<Result<string>> PairAsync(Guid deviceId, Guid householdId, string name, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("internal/devices", new
        {
            clientId = configuration["Devices:ClientId"] ?? "devices-service",
            clientSecret = configuration["Devices:ClientSecret"] ?? string.Empty,
            deviceId,
            householdId,
            name,
        }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Result.Fail(Unavailable);
        }

        var paired = await response.Content.ReadFromJsonAsync<PairedIdentityResponse>(cancellationToken);
        return paired is { Secret.Length: > 0 } ? paired.Secret : Result.Fail(Unavailable);
    }
}
