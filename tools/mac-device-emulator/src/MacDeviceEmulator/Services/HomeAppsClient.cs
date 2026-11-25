using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;
using MacDeviceEmulator.Configuration;
using MacDeviceEmulator.Protocol;

namespace MacDeviceEmulator.Services;

public sealed record PairResponse(
    [property: JsonPropertyName("deviceId")] Guid DeviceId,
    [property: JsonPropertyName("householdId")] Guid HouseholdId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("secret")] string Secret,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("purpose")] string Purpose);

public sealed record TokenResponse(
    [property: JsonPropertyName("accessToken")] string Token,
    [property: JsonPropertyName("expiresAt")] DateTimeOffset ExpiresAt);

public interface IHomeAppsClient
{
    Task<Result<PairResponse>> PairAsync(string serverUrl, string code, string descriptorJson, CancellationToken cancellationToken = default);

    Task<Result<string>> GetDeviceTokenAsync(string serverUrl, Guid deviceId, string secret, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FrameResult>>> SendReportAsync(string serverUrl, string token, IReadOnlyList<DeviceFrame> frames, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<VoiceActionView>>> GetPendingConfirmationsAsync(string serverUrl, string userToken, CancellationToken cancellationToken = default);

    Task<Result<VoiceActionView>> DecideConfirmationAsync(string serverUrl, string userToken, Guid actionId, bool approve, CancellationToken cancellationToken = default);
}

public sealed class HomeAppsClient(HttpClient httpClient) : IHomeAppsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<Result<PairResponse>> PairAsync(string serverUrl, string code, string descriptorJson, CancellationToken cancellationToken = default)
    {
        var url = $"{serverUrl.TrimEnd('/')}/devices/v1/pair";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        using var doc = JsonDocument.Parse(descriptorJson);
        var body = new
        {
            code,
            descriptor = doc.RootElement
        };

        request.Content = JsonContent.Create(body);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Fail($"Pairing failed with status {response.StatusCode}: {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<PairResponse>(JsonOptions, cancellationToken);
        return result is not null ? Result.Ok(result) : Result.Fail("Empty pairing response");
    }

    public async Task<Result<string>> GetDeviceTokenAsync(string serverUrl, Guid deviceId, string secret, CancellationToken cancellationToken = default)
    {
        var url = $"{serverUrl.TrimEnd('/')}/auth/v1/devices/token";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        var body = new
        {
            deviceId,
            secret
        };

        request.Content = JsonContent.Create(body);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (content.Contains("devices.device_unpaired", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Fail("devices.device_unpaired");
            }

            return Result.Fail("devices.invalid_credentials");
        }

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Fail($"Token request failed with status {response.StatusCode}: {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken);
        return result is not null ? Result.Ok(result.Token) : Result.Fail("Empty token response");
    }

    public async Task<Result<IReadOnlyList<FrameResult>>> SendReportAsync(string serverUrl, string token, IReadOnlyList<DeviceFrame> frames, CancellationToken cancellationToken = default)
    {
        var url = $"{serverUrl.TrimEnd('/')}/devices/v1/report";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(frames);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (content.Contains("devices.device_unpaired", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Fail("devices.device_unpaired");
            }
            return Result.Fail("devices.invalid_credentials");
        }

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Fail($"Report request failed with status {response.StatusCode}: {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<List<FrameResult>>(JsonOptions, cancellationToken);
        return result is not null ? Result.Ok<IReadOnlyList<FrameResult>>(result) : Result.Fail("Empty report response");
    }

    public async Task<Result<IReadOnlyList<VoiceActionView>>> GetPendingConfirmationsAsync(string serverUrl, string userToken, CancellationToken cancellationToken = default)
    {
        var url = $"{serverUrl.TrimEnd('/')}/jarvis/api/v1/confirmations?pending=true";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Fail($"Confirmations request failed with status {response.StatusCode}: {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<List<VoiceActionView>>(JsonOptions, cancellationToken);
        return result is not null ? Result.Ok<IReadOnlyList<VoiceActionView>>(result) : Result.Ok<IReadOnlyList<VoiceActionView>>([]);
    }

    public async Task<Result<VoiceActionView>> DecideConfirmationAsync(string serverUrl, string userToken, Guid actionId, bool approve, CancellationToken cancellationToken = default)
    {
        var url = $"{serverUrl.TrimEnd('/')}/jarvis/api/v1/confirmations/{actionId}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        request.Content = JsonContent.Create(new { approve });

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Fail($"Confirmation decision failed with status {response.StatusCode}: {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<VoiceActionView>(JsonOptions, cancellationToken);
        return result is not null ? Result.Ok(result) : Result.Fail("Empty confirmation response");
    }
}
