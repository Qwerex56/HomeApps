using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Jarvis.VoiceBridge;

/// <summary>
/// The bridge signs in like Claude Code does (docs/auth.md "Connected apps"): it finds Auth from the gateway's metadata,
/// registers itself once, opens the browser for "Zezwól" with PKCE and a loopback redirect, and then lives on the
/// refresh token. It shows up in "Połączone aplikacje", where it can be disconnected.
/// </summary>
internal sealed class OAuthLogin(HttpClient http, BridgeOptions options, TokenStore store) : IAccessToken
{
    private const string ClientKey = "client_id";
    private const string RefreshKey = "refresh_token";
    private const string Callback = "/callback";

    public string Kind => "computer";

    private string? _access;
    private DateTimeOffset _expires;

    public async Task<string> AccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_access is not null && _expires > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _access;
        }

        var server = await DiscoverAsync(cancellationToken);
        if (store.Read(RefreshKey) is { } refresh && store.Read(ClientKey) is { } client)
        {
            var refreshed = await TokenAsync(server, new()
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refresh,
                ["client_id"] = client,
                ["resource"] = server.Resource,
            }, cancellationToken);
            if (refreshed)
            {
                return _access!;
            }

            store.Delete(RefreshKey);
        }

        await SignInAsync(server, cancellationToken);
        return _access!;
    }

    public void Forget()
    {
        store.Delete(RefreshKey);
        store.Delete(ClientKey);
    }

    private async Task SignInAsync(Server server, CancellationToken cancellationToken)
    {
        var port = FreePort();
        var redirect = $"http://127.0.0.1:{port}{Callback}";
        var client = store.Read(ClientKey) ?? await RegisterAsync(server, cancellationToken);
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(16));
        var authorize = $"{server.Authorize}?response_type=code&client_id={Uri.EscapeDataString(client)}"
            + $"&redirect_uri={Uri.EscapeDataString(redirect)}&code_challenge={challenge}&code_challenge_method=S256"
            + $"&state={state}&scope=homeapps&resource={Uri.EscapeDataString(server.Resource)}";

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        Console.WriteLine("Otwieram przeglądarkę: zaloguj się w HomeApps i kliknij „Zezwól”. Jeśli się nie otworzyła:");
        Console.WriteLine(authorize);
        Open(authorize);

        var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromMinutes(5), cancellationToken);
        var query = context.Request.QueryString;
        var ok = query["state"] == state && query["code"] is not null;
        var page = Encoding.UTF8.GetBytes(ok
            ? "<html><body><h3>Gotowe. JARVIS słucha, możesz zamknąć to okno.</h3></body></html>"
            : "<html><body><h3>Nie udało się połączyć. Spróbuj jeszcze raz w terminalu.</h3></body></html>");
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.OutputStream.WriteAsync(page, cancellationToken);
        context.Response.Close();
        if (!ok)
        {
            throw new InvalidOperationException($"Sign-in refused: {query["error"] ?? "wrong state"}.");
        }

        var signedIn = await TokenAsync(server, new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = query["code"]!,
            ["redirect_uri"] = redirect,
            ["client_id"] = client,
            ["code_verifier"] = verifier,
            ["resource"] = server.Resource,
        }, cancellationToken);
        if (!signedIn)
        {
            throw new InvalidOperationException("Auth did not give a token for the code.");
        }
    }

    private async Task<string> RegisterAsync(Server server, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(server.Register, new JsonObject
        {
            ["client_name"] = $"JARVIS: most głosowy ({options.Device})",
            ["redirect_uris"] = new JsonArray($"http://127.0.0.1{Callback}"),
            ["grant_types"] = new JsonArray("authorization_code", "refresh_token"),
            ["response_types"] = new JsonArray("code"),
            ["token_endpoint_auth_method"] = "none",
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var registered = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken);
        var client = registered?["client_id"]?.GetValue<string>() ?? throw new InvalidOperationException("No client_id.");
        store.Write(ClientKey, client);
        return client;
    }

    private async Task<bool> TokenAsync(Server server, Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await http.PostAsync(server.Token, content, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken);
        if (!response.IsSuccessStatusCode || body?["access_token"] is null)
        {
            if (body?["error"]?.GetValue<string>() == "invalid_client")
            {
                store.Delete(ClientKey);
            }

            return false;
        }

        _access = body["access_token"]!.GetValue<string>();
        _expires = DateTimeOffset.UtcNow.AddSeconds(body["expires_in"]?.GetValue<int>() ?? 900);
        if (body["refresh_token"]?.GetValue<string>() is { } refresh)
        {
            store.Write(RefreshKey, refresh);
        }

        return true;
    }

    /// <summary>RFC 9728 then RFC 8414: the gateway names its resource and its Auth, Auth names its endpoints.</summary>
    private async Task<Server> DiscoverAsync(CancellationToken cancellationToken)
    {
        var resource = await http.GetFromJsonAsync<JsonObject>(
            new Uri(options.Server, ".well-known/oauth-protected-resource/jarvis/mcp"), cancellationToken)
            ?? throw new InvalidOperationException("The server has no JARVIS gateway metadata.");
        var issuer = resource["authorization_servers"]![0]!.GetValue<string>().TrimEnd('/');
        var auth = await http.GetFromJsonAsync<JsonObject>(new Uri(issuer + "/.well-known/oauth-authorization-server"), cancellationToken)
            ?? throw new InvalidOperationException("Auth has no OAuth metadata.");
        return new Server(
            resource["resource"]!.GetValue<string>(),
            new Uri(auth["authorization_endpoint"]!.GetValue<string>()),
            new Uri(auth["token_endpoint"]!.GetValue<string>()),
            new Uri(auth["registration_endpoint"]!.GetValue<string>()));
    }

    private static int FreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        return ((IPEndPoint)socket.LocalEndpoint).Port;
    }

    private static void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(OperatingSystem.IsMacOS() ? "open" : "xdg-open") { ArgumentList = { url } })?.Dispose();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // No browser opener: the URL is printed above.
        }
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record Server(string Resource, Uri Authorize, Uri Token, Uri Register);
}
