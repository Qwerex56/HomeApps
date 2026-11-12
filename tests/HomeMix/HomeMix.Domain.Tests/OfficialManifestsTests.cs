using HomeMix.Domain.Manifests;

namespace HomeMix.Domain.Tests;

/// <summary>The official add-ons in homemix/official pass the same checks as an upload.</summary>
public sealed class OfficialManifestsTests
{
    public static TheoryData<string> Official()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "homemix", "official")))
        {
            dir = dir.Parent;
        }

        var data = new TheoryData<string>();
        foreach (var addon in Directory.GetDirectories(Path.Combine(dir!.FullName, "homemix", "official")))
        {
            data.Add(Path.Combine(addon, "public", "homemix.json"));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Official))]
    public void Each_official_manifest_is_valid(string path)
    {
        var result = AddonManifestParser.Parse(File.ReadAllText(path));

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Errors.Select(e => e.Message)));
    }
}
