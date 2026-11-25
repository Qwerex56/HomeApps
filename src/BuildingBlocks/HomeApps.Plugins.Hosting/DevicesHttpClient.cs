using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Plugins.Devices;
using HomeApps.Plugins.Hosting.Devices;
using HomeApps.Plugins.Manifest;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HomeApps.Plugins.Hosting;

/// <summary>
/// <see cref="IDevicesClient"/> through Devices' REST API (docs/devices.md) with the caller's own token, so Devices
/// applies exactly its own rules. A missing household or device reads as empty or <c>null</c>; Devices' refusals of
/// <c>set</c> come back as failed results with its error code; anything unexpected throws, like the Households clients.
/// </summary>
internal sealed class DevicesHttpClient(HttpClient http, IHttpContextAccessor accessor, PluginManifest manifest) : IDevicesClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<PluginDevice>> ListAsync(Guid householdId, DeviceFilter? filter, bool withState, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, ListPath(householdId, filter, withState), null, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return [];
        }

        await EnsureAsync(response);
        var devices = await response.Content.ReadFromJsonAsync<List<DeviceResponse>>(Json, cancellationToken);
        return [.. (devices ?? []).Where(d => !d.Removed).Select(d => d.ToDevice())];
    }

    public async Task<PluginDeviceState?> GetStateAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, $"devices/v1/{deviceId}/state", null, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }

        await EnsureAsync(response);
        return (await response.Content.ReadFromJsonAsync<StateResponse>(Json, cancellationToken))?.ToState();
    }

    public async Task<Result> SetAsync(Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken)
    {
        using var content = JsonContent.Create(new { control, value }, options: Json);
        using var response = await SendAsync(HttpMethod.Post, $"devices/v1/{deviceId}/set", content, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return Result.Ok();
        }

        if (response.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable)
        {
            await EnsureAsync(response);
        }

        return Result.Fail(await ErrorAsync(response, cancellationToken));
    }

    /// <summary>The path and query of the household's list: only the filters that are set, and <c>state=true</c> on request.</summary>
    internal static string ListPath(Guid householdId, DeviceFilter? filter, bool withState)
    {
        var query = new List<string>();
        if (filter?.Capability is { Length: > 0 } capability)
        {
            query.Add($"capability={Uri.EscapeDataString(capability)}");
        }

        if (filter?.Role is { Length: > 0 } role)
        {
            query.Add($"role={Uri.EscapeDataString(role)}");
        }

        if (filter?.Model is { Length: > 0 } model)
        {
            query.Add($"model={Uri.EscapeDataString(model)}");
        }

        if (withState)
        {
            query.Add("state=true");
        }

        var path = $"devices/v1/households/{householdId}";
        return query.Count == 0 ? path : $"{path}?{string.Join('&', query)}";
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        var authorization = CallerAuthorization.Read(accessor, manifest)
            ?? throw new InvalidOperationException("The devices client needs the caller's access token; there is no HTTP request with one.");

        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, authorization);
        return await http.SendAsync(request, cancellationToken);
    }

    private static Task EnsureAsync(HttpResponseMessage response) =>
        response.IsSuccessStatusCode
            ? Task.CompletedTask
            : throw new HttpRequestException($"Devices answered {(int)response.StatusCode}.", null, response.StatusCode);

    private static async Task<Error> ErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string? code = null;
        string? title = null;
        try
        {
            using var problem = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            code = problem.RootElement.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
            title = problem.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
        }
        catch (JsonException)
        {
            // Not a problem document: the status tells enough.
        }

        var kind = response.StatusCode switch
        {
            HttpStatusCode.NotFound => ErrorKind.NotFound,
            HttpStatusCode.Forbidden => ErrorKind.Forbidden,
            HttpStatusCode.Conflict => ErrorKind.Conflict,
            _ => ErrorKind.Validation,
        };
        return new AppError(code ?? "devices.request_failed", title ?? $"Devices answered {(int)response.StatusCode}.", kind);
    }
}
