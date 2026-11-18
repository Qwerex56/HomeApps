using System.Text.Json;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;

namespace Devices.Domain.Tests;

public sealed class FrameValidationTests
{
    private const string Chair = """
        { "format": "homeapps.device/1", "model": "a/fotel", "version": "1.0.0", "purpose": "smart_home",
          "controls": [
            { "name": "heating", "value": { "type": "integer", "minimum": 0, "maximum": 3 } },
            { "name": "mode", "value": { "type": "string", "enum": ["eco", "boost"] } } ],
          "sensors": [ { "name": "temperature", "value": { "type": "number" } } ],
          "events": [
            { "name": "button", "data": { "type": "object", "properties": { "count": { "type": "integer" } }, "required": ["count"] } },
            { "name": "tag", "location": true } ] }
        """;

    private static readonly DeviceDescriptor Descriptor = DescriptorParser.Parse(Chair).Value;

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void State_of_declared_controls_and_sensors_is_accepted()
    {
        Assert.True(FrameValidation.CheckState(Descriptor, Json("""{ "heating": 2, "temperature": 21.5, "mode": "eco" }""")).IsSuccess);
    }

    [Theory]
    [InlineData("""{ "heating": 7 }""")]
    [InlineData("""{ "heating": 1.5 }""")]
    [InlineData("""{ "heating": "hot" }""")]
    [InlineData("""{ "mode": "turbo" }""")]
    [InlineData("""{ "unknown": 1 }""")]
    [InlineData("""{ }""")]
    [InlineData("""[1]""")]
    public void State_that_breaks_the_descriptor_is_refused(string values)
    {
        Assert.True(FrameValidation.CheckState(Descriptor, Json(values)).IsFailed);
    }

    [Fact]
    public void State_reports_every_problem_at_once()
    {
        var result = FrameValidation.CheckState(Descriptor, Json("""{ "heating": 9, "unknown": 1 }"""));

        Assert.Contains("'heating'", result.Errors[0].Message, StringComparison.Ordinal);
        Assert.Contains("'unknown'", result.Errors[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_set_needs_a_control_and_a_value_that_fits()
    {
        Assert.True(FrameValidation.CheckSet(Descriptor, "heating", Json("3")).IsSuccess);
        Assert.True(FrameValidation.CheckSet(Descriptor, "heating", Json("4")).IsFailed);
        Assert.True(FrameValidation.CheckSet(Descriptor, "heating", null).IsFailed);
        Assert.Equal("devices.control_not_found", ((HomeApps.BuildingBlocks.Domain.Errors.AppError)FrameValidation.CheckSet(Descriptor, "temperature", Json("1")).Errors[0]).Code);
    }

    [Fact]
    public void An_event_must_be_declared_and_its_data_must_fit()
    {
        Assert.True(FrameValidation.CheckEvent(Descriptor, "button", Json("""{ "count": 1 }""")).IsSuccess);
        Assert.True(FrameValidation.CheckEvent(Descriptor, "button", Json("""{ "count": "x" }""")).IsFailed);
        Assert.True(FrameValidation.CheckEvent(Descriptor, "button", Json("""{ }""")).IsFailed);
        Assert.True(FrameValidation.CheckEvent(Descriptor, "tag", null).IsSuccess);
        Assert.True(FrameValidation.CheckEvent(Descriptor, "doorbell", null).IsFailed);
    }

    [Fact]
    public void An_event_is_a_location_event_when_declared_or_on_a_geo_device()
    {
        Assert.False(Descriptor.IsLocation(Descriptor.Events[0]));
        Assert.True(Descriptor.IsLocation(Descriptor.Events[1]));
        var geo = DescriptorParser.Parse(Chair.Replace("\"purpose\": \"smart_home\",", "\"purpose\": \"smart_home\", \"capabilities\": [ { \"type\": \"geo\" } ],", StringComparison.Ordinal)).Value;
        Assert.True(geo.IsLocation(geo.Events[0]));
    }

    [Theory]
    [InlineData(null, 60)]
    [InlineData(120, 120)]
    public void The_heartbeat_defaults_to_a_minute_on_a_permanent_link(int? declared, int expected)
    {
        var json = Chair.Replace("\"purpose\": \"smart_home\",", "\"purpose\": \"smart_home\"," + (declared is null ? "" : $" \"heartbeat\": {declared},"), StringComparison.Ordinal);

        Assert.Equal(expected, DescriptorParser.Parse(json).Value.HeartbeatSeconds);
    }

    [Fact]
    public void A_sleepy_device_without_a_declared_period_gets_fifteen_minutes()
    {
        var json = Chair.Replace("\"purpose\": \"smart_home\",", "\"purpose\": \"smart_home\", \"sleepy\": true,", StringComparison.Ordinal);

        Assert.Equal(900, DescriptorParser.Parse(json).Value.HeartbeatSeconds);
    }

    [Theory]
    [InlineData(101.0, null)]
    [InlineData(-1.0, null)]
    [InlineData(null, -5)]
    public void A_status_with_absurd_numbers_is_refused(double? battery, int? uptime)
    {
        Assert.True(new StatusReport(battery, null, null, uptime, null, null).Check().IsFailed);
    }

    [Fact]
    public void Location_events_are_kept_for_the_shorter_time()
    {
        var now = new DateTimeOffset(2026, 11, 18, 0, 0, 0, TimeSpan.Zero);
        var retention = new EventRetention(30, 7);

        Assert.Equal(now.AddDays(-30), retention.CutoffFor(isLocation: false, now));
        Assert.Equal(now.AddDays(-7), retention.CutoffFor(isLocation: true, now));
        Assert.Equal(now.AddDays(-10), new EventRetention(10, 40).CutoffFor(isLocation: true, now));
    }

    [Fact]
    public void State_with_a_nul_is_refused()
    {
        Assert.True(FrameValidation.CheckState(Descriptor, Json("""{ "mode": "a\u0000b" }""")).IsFailed);
    }

    [Fact]
    public void Event_data_with_a_nul_is_refused()
    {
        Assert.True(FrameValidation.CheckEvent(Descriptor, "button", Json("""{ "count": 1, "x": "\u0000" }""")).IsFailed);
    }
}
