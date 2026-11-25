using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Plugins.Devices;
using HomeApps.Plugins.Hosting;
using Microsoft.AspNetCore.Http;

namespace HomeApps.Plugins.Tests;

public sealed class DevicesClientTests
{
    private static readonly Guid Household = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Device = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly string[] StationRoles = ["weather_station"];
    private static readonly string[] StationCapabilities = ["weather", "power"];

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return answer(request);
        }
    }

    private static DevicesHttpClient ClientOver(Handler handler, string? authorization = "Bearer abc")
    {
        var context = new DefaultHttpContext();
        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        var http = new HttpClient(handler) { BaseAddress = new Uri("http://devices/") };
        return new DevicesHttpClient(http, new HttpContextAccessor { HttpContext = context }, TestManifests.Valid);
    }

    private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(body, options: new JsonSerializerOptions(JsonSerializerDefaults.Web)) };

    [Fact]
    public void The_list_path_carries_only_the_filters_that_are_set()
    {
        Assert.Equal($"devices/v1/households/{Household}", DevicesHttpClient.ListPath(Household, null, false));
        Assert.Equal(
            $"devices/v1/households/{Household}?capability=weather&role=weather_station&model=a%2Fstacja&state=true",
            DevicesHttpClient.ListPath(Household, new DeviceFilter("weather", "weather_station", "a/stacja"), true));
        Assert.Equal($"devices/v1/households/{Household}?role=light", DevicesHttpClient.ListPath(Household, new DeviceFilter(Role: "light"), false));
    }

    [Fact]
    public async Task List_sends_the_callers_token_and_maps_devices_without_the_removed_ones()
    {
        var handler = new Handler(_ => Json(new object[]
        {
            new
            {
                id = Device, name = "Stacja", slug = "stacja", purpose = "smart_home", online = true, removed = false,
                descriptor = new { model = "a/stacja", roles = StationRoles, capabilities = StationCapabilities },
                state = new { deviceId = Device, values = new { temperature = 21.5 }, online = true, silent = false, lowBattery = false },
            },
            new { id = Guid.NewGuid(), name = "Stara", slug = "stara", purpose = "smart_home", online = false, removed = true, descriptor = new { roles = Array.Empty<string>(), capabilities = Array.Empty<string>() } },
        }));

        var devices = await ClientOver(handler).ListAsync(Household, new DeviceFilter("weather"), true, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Equal($"/devices/v1/households/{Household}?capability=weather&state=true", handler.Request.RequestUri!.PathAndQuery);
        Assert.Equal("Bearer abc", handler.Request.Headers.Authorization!.ToString());
        var device = Assert.Single(devices);
        Assert.Equal("a/stacja", device.Model);
        Assert.Equal(["weather_station"], device.Roles);
        Assert.Equal(["weather", "power"], device.Capabilities);
        Assert.Equal(21.5, device.State!.Values.GetProperty("temperature").GetDouble());
    }

    [Fact]
    public async Task A_household_without_access_lists_nothing_and_a_missing_device_has_no_state()
    {
        var client = ClientOver(new Handler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        Assert.Empty(await client.ListAsync(Household, null, false, CancellationToken.None));
        Assert.Null(await client.GetStateAsync(Device, CancellationToken.None));
    }

    [Fact]
    public async Task State_is_read_from_the_device_state_endpoint()
    {
        var handler = new Handler(_ => Json(new { deviceId = Device, values = new { on = true }, valuesAt = (DateTimeOffset?)null, online = true, silent = false, lowBattery = true, battery = 12.0 }));

        var state = await ClientOver(handler).GetStateAsync(Device, CancellationToken.None);

        Assert.Equal($"/devices/v1/{Device}/state", handler.Request!.RequestUri!.AbsolutePath);
        Assert.True(state!.LowBattery);
        Assert.True(state.Values.GetProperty("on").GetBoolean());
    }

    [Fact]
    public async Task Set_posts_the_control_and_value()
    {
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await ClientOver(handler).SetAsync(Device, "level", JsonDocument.Parse("42").RootElement, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal($"/devices/v1/{Device}/set", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("""{"control":"level","value":42}""", handler.Body);
    }

    [Fact]
    public async Task A_refused_set_fails_with_devices_error_code()
    {
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("""{"title":"x","detail":"The device is offline.","code":"devices.offline"}""", System.Text.Encoding.UTF8, "application/problem+json"),
        });

        var result = await ClientOver(handler).SetAsync(Device, "on", JsonDocument.Parse("true").RootElement, CancellationToken.None);

        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal("devices.offline", error.Code);
        Assert.Equal(ErrorKind.Conflict, error.Kind);
    }

    [Fact]
    public async Task Without_a_token_the_client_refuses_to_ask()
    {
        var client = ClientOver(new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK)), authorization: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ListAsync(Household, null, false, CancellationToken.None));
    }
}
