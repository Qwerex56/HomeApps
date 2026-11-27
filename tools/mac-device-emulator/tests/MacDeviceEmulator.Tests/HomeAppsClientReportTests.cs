using System.Net;
using System.Text;
using FluentResults;
using MacDeviceEmulator.Protocol;
using MacDeviceEmulator.Services;
using Xunit;

namespace MacDeviceEmulator.Tests;

/// <summary>jarvis-desktop-rework 2.8: POST /devices/v1/report answers {"results": [...], "settings": {...}|null}.</summary>
public sealed class HomeAppsClientReportTests
{
    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }

    private static async Task<Result<ReportResponse>> ReportAsync(string body)
    {
        var client = new HomeAppsClient(new HttpClient(new StubHandler(body)));
        return await client.SendReportAsync("http://localhost:5000", "token", [new DeviceFrame("status")]);
    }

    [Fact]
    public async Task The_results_are_read_from_the_wrapped_answer_with_settings()
    {
        var result = await ReportAsync("""{ "results": [ { "ok": true } ], "settings": { "heating": 3 } }""");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Results!);
        Assert.True(result.Value.Results![0].Ok);
        Assert.Equal(3, result.Value.Settings!.Value.GetProperty("heating").GetInt32());
    }

    [Fact]
    public async Task A_null_settings_answer_still_yields_the_results()
    {
        var result = await ReportAsync("""{ "results": [ { "ok": false, "code": "devices.invalid_frame", "text": "bad" } ], "settings": null }""");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Results![0].Ok);
    }
}
