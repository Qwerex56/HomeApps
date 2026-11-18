using System.Net;
using System.Net.Http.Json;

namespace Jarvis.VoiceBridge;

/// <summary>
/// The bridge as a household satellite (jarvis-satellites.md S-1e, S13): <c>--pair ABCD-EFGH</c> trades the code an admin
/// got on the Urządzenia page for the device's id and secret, kept like the person's token (the Keychain on macOS).
/// From then on the bridge trades the secret for short tokens and speaks as a member of that household.
/// </summary>
internal sealed class SatelliteLogin(HttpClient http, BridgeOptions options, TokenStore store) : IAccessToken
{
    private const string DeviceKey = "satellite_device";
    private const string SecretKey = "satellite_secret";

    private string? _access;
    private DateTimeOffset _expires;

    public string Kind => "satellite";

    public static bool IsPaired(TokenStore store) => store.Read(DeviceKey) is not null && store.Read(SecretKey) is not null;

    /// <summary>Spends the code. A used or expired one is refused (404 <c>devices.pairing_expired</c>).</summary>
    public async Task<PairedDevice> PairAsync(string code, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(new Uri(options.Server, "devices/pair"), new { code }, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException("Ten kod nie działa: wygasł, został użyty albo jest błędny. Poproś admina domu o nowy.");
        }

        response.EnsureSuccessStatusCode();
        var paired = await response.Content.ReadFromJsonAsync<PairedDevice>(cancellationToken)
            ?? throw new InvalidOperationException("Devices answered the pairing without the device.");
        store.Write(DeviceKey, paired.DeviceId.ToString());
        store.Write(SecretKey, paired.Secret);
        _access = null;
        return paired;
    }

    public async Task<string> AccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_access is not null && _expires > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _access;
        }

        var deviceId = store.Read(DeviceKey) ?? throw Unpaired();
        var secret = store.Read(SecretKey) ?? throw Unpaired();
        using var response = await http.PostAsJsonAsync(
            new Uri(options.Server, "auth/devices/token"), new { deviceId = Guid.Parse(deviceId), secret }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Removed on the Urządzenia page or given a new code: this secret is over.
            Forget();
            throw Unpaired();
        }

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<DeviceToken>(cancellationToken)
            ?? throw new InvalidOperationException("Auth answered without a token.");
        (_access, _expires) = (token.AccessToken, token.ExpiresAt);
        return _access;
    }

    public void Forget()
    {
        store.Delete(DeviceKey);
        store.Delete(SecretKey);
        _access = null;
    }

    private static InvalidOperationException Unpaired() =>
        new("Ta satelita nie jest już sparowana z domem. Admin daje nowy kod na stronie Urządzenia: uruchom most z --pair <kod>.");
}
