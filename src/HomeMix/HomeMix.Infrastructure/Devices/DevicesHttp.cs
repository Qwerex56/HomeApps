using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeMix.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HomeMix.Infrastructure.Devices;

/// <summary>
/// Asks Devices (docs/devices.md) with the caller's own token, so Devices applies its own rules: a guest reads, a member
/// controls. A household or device the caller cannot see reads as empty or <c>null</c>; Devices' refusals of <c>set</c>
/// come back as failed results with its error code; anything unexpected throws, like <c>HouseholdsHttpRoles</c>.
/// </summary>
internal sealed class DevicesHttp(HttpClient http, IHttpContextAccessor accessor) : IHouseholdDevices
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<HouseholdDevice>> ListAsync(
        Guid householdId, string? capability, string? role, string? model, bool withState, CancellationToken cancellationToken)
    {
        using var request = Request(HttpMethod.Get, ListPath(householdId, capability, role, model, withState));
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return [];
        }

        EnsureSuccess(response);
        var devices = await response.Content.ReadFromJsonAsync<List<DeviceResponse>>(Json, cancellationToken);
        return [.. (devices ?? []).Where(d => !d.Removed).Select(d => d.ToDevice())];
    }

    public async Task<JsonElement?> GetStateAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        using var request = Request(HttpMethod.Get, $"devices/v1/{deviceId}/state");
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }

        EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json, cancellationToken);
    }

    public async Task<Result> SetAsync(Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken)
    {
        using var request = Request(HttpMethod.Post, $"devices/v1/{deviceId}/set");
        request.Content = JsonContent.Create(new { control, value }, options: Json);
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return Result.Ok();
        }

        if ((int)response.StatusCode >= 500)
        {
            EnsureSuccess(response);
        }

        return Result.Fail(await ErrorAsync(response, cancellationToken));
    }

    internal static string ListPath(Guid householdId, string? capability, string? role, string? model, bool withState)
    {
        var query = new List<string>();
        AddIfSet(query, "capability", capability);
        AddIfSet(query, "role", role);
        AddIfSet(query, "model", model);
        if (withState)
        {
            query.Add("state=true");
        }

        var path = $"devices/v1/households/{householdId}";
        return query.Count == 0 ? path : $"{path}?{string.Join('&', query)}";
    }

    private static void AddIfSet(List<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var authorization = accessor.HttpContext?.Request.Headers[HeaderNames.Authorization].ToString();
        if (string.IsNullOrEmpty(authorization))
        {
            throw new InvalidOperationException("Devices is asked with the caller's access token; there is no HTTP request with one.");
        }

        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, authorization);
        return request;
    }

    internal static void EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Devices answered {(int)response.StatusCode}.", null, response.StatusCode);
        }
    }

    internal static async Task<Error> ErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string? code = null;
        string? detail = null;
        try
        {
            using var problem = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            code = problem.RootElement.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
            detail = problem.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
        }
        catch (JsonException)
        {
            // Not a problem document: the status says enough.
        }

        var kind = response.StatusCode switch
        {
            HttpStatusCode.NotFound => ErrorKind.NotFound,
            HttpStatusCode.Forbidden => ErrorKind.Forbidden,
            HttpStatusCode.Conflict => ErrorKind.Conflict,
            _ => ErrorKind.Validation,
        };
        return new AppError(code ?? "devices.request_failed", detail ?? $"Devices answered {(int)response.StatusCode}.", kind);
    }
}
