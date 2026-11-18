using System.Text.Json;
using System.Text.RegularExpressions;
using Devices.Domain.Capabilities;
using Devices.Domain.Descriptors;

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
        var resolution = CapabilityResolver.Resolve(parsed.Value, CapabilityCatalog.BuiltIn);
        Assert.Empty(resolution.Warnings);
        Assert.Equal(parsed.Value.Capabilities.Count, resolution.Accepted.Count);
        Assert.Equal(parsed.Value.Roles.Count, resolution.Roles.Count);
    }

    private static List<string> Descriptors()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !File.Exists(Path.Combine(directory, "HomeApps.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        Assert.NotNull(directory);
        var text = File.ReadAllText(Path.Combine(directory, "docs", "devices-protocol.md"));
        var result = new List<string>();
        foreach (Match block in JsonBlock().Matches(text))
        {
            var json = block.Groups[1].Value;
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("format", out _))
            {
                result.Add(json);
            }
        }

        return result;
    }

    [GeneratedRegex("```json\\r?\\n(.*?)```", RegexOptions.Singleline)]
    private static partial Regex JsonBlock();
}
