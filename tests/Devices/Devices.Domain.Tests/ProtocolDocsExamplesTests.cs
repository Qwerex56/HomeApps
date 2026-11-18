using System.Text.Json;
using System.Text.RegularExpressions;
using Devices.Domain.Capabilities;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;

namespace Devices.Domain.Tests;

/// <summary>Every descriptor example in docs/devices-protocol.md has to be accepted by the parser, without warnings.</summary>
public sealed partial class ProtocolDocsExamplesTests
{
    public static TheoryData<int> Examples()
    {
        var data = new TheoryData<int>();
        for (var i = 0; i < Descriptors().Count; i++)
        {
            data.Add(i);
        }

        return data;
    }

    [Fact]
    public void The_document_has_descriptor_examples()
    {
        Assert.True(Descriptors().Count >= 3);
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public void A_documented_descriptor_is_valid_and_resolves_without_warnings(int index)
    {
        var json = Descriptors()[index];

        var parsed = DescriptorParser.Parse(json);

        Assert.True(parsed.IsSuccess, string.Join("; ", parsed.Errors.Select(e => e.Message)));
        var resolution = CapabilityResolver.Resolve(parsed.Value, CapabilityCatalog.BuiltIn, new Dictionary<string, RegisteredCapability>(), isHouseholdDevice: true);
        Assert.Empty(resolution.Warnings);
        Assert.Equal(parsed.Value.Capabilities.Count, resolution.Accepted.Count);
        Assert.Equal(parsed.Value.Roles.Count, resolution.Roles.Count);
    }

    [Fact]
    public void The_documented_state_set_and_event_frames_fit_the_armchair_descriptor()
    {
        var chair = DescriptorParser.Parse(Descriptor("qwerex56/fotel-v1")).Value;

        var state = Frame("state");
        Assert.True(FrameValidation.CheckState(chair, state.GetProperty("values")).IsSuccess);

        var set = Frame("set");
        Assert.True(FrameValidation.CheckSet(chair, set.GetProperty("name").GetString(), set.GetProperty("value")).IsSuccess);

        var @event = Frame("event");
        Assert.True(FrameValidation.CheckEvent(chair, @event.GetProperty("name").GetString(), @event.GetProperty("data")).IsSuccess);
        Assert.True(DateTimeOffset.TryParse(@event.GetProperty("at").GetString(), out _));
    }

    [Fact]
    public void The_documented_status_frame_is_a_valid_heartbeat()
    {
        var status = Frame("status");

        var report = new StatusReport(
            status.GetProperty("battery").GetDouble(), status.GetProperty("voltage").GetDouble(), status.GetProperty("rssi").GetInt32(),
            status.GetProperty("uptime").GetInt64(), status.GetProperty("firmware").GetString(), null);

        Assert.True(report.Check().IsSuccess);
    }

    [Fact]
    public void The_documented_report_fits_the_weather_station_descriptor()
    {
        var station = DescriptorParser.Parse(Descriptor("qwerex56/stacja-v2")).Value;
        var report = Blocks().Select(b => JsonDocument.Parse(b).RootElement)
            .Single(r => r.ValueKind == JsonValueKind.Array && r.GetArrayLength() > 0 && r[0].TryGetProperty("type", out _));

        Assert.InRange(report.GetArrayLength(), 1, 50);
        foreach (var frame in report.EnumerateArray())
        {
            switch (frame.GetProperty("type").GetString())
            {
                case "state":
                    Assert.True(FrameValidation.CheckState(station, frame.GetProperty("values")).IsSuccess);
                    break;
                case "event":
                    frame.TryGetProperty("data", out var data);
                    Assert.True(FrameValidation.CheckEvent(station, frame.GetProperty("name").GetString(), data.ValueKind == JsonValueKind.Undefined ? null : data).IsSuccess);
                    break;
                case "status":
                    var battery = frame.GetProperty("battery").GetDouble();
                    Assert.True(new StatusReport(battery, null, frame.GetProperty("rssi").GetInt32(), null, null, null).Check().IsSuccess);
                    break;
                default:
                    Assert.Fail("unknown frame type in the documented report");
                    break;
            }
        }
    }

    private static string Descriptor(string model) => Descriptors().Single(d => d.Contains($"\"{model}\"", StringComparison.Ordinal));

    private static JsonElement Frame(string type) => Blocks().Select(b => JsonDocument.Parse(b).RootElement.Clone())
        .Single(r => r.ValueKind == JsonValueKind.Object && r.TryGetProperty("type", out var t) && t.GetString() == type);

    private static List<string> Blocks()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !File.Exists(Path.Combine(directory, "HomeApps.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        Assert.NotNull(directory);
        var text = File.ReadAllText(Path.Combine(directory, "docs", "devices-protocol.md"));
        return JsonBlock().Matches(text).Select(m => m.Groups[1].Value).ToList();
    }

    private static List<string> Descriptors()
    {
        var result = new List<string>();
        foreach (var json in Blocks())
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement is { ValueKind: JsonValueKind.Object } root && root.TryGetProperty("format", out _))
            {
                result.Add(json);
            }
        }

        return result;
    }

    [GeneratedRegex("```json\\r?\\n(.*?)```", RegexOptions.Singleline)]
    private static partial Regex JsonBlock();
}
