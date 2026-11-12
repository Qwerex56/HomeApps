using HomeMix.Domain.Manifests;
using HomeMix.Domain.Packages;

namespace HomeMix.Domain.Tests;

public sealed class PackageRulesTests
{
    private static readonly AddonManifest Manifest = AddonManifestParser.Parse(Manifests.Valid()).Value;

    [Theory]
    [InlineData("index.html", true)]
    [InlineData("assets/app.js", true)]
    [InlineData("../index.html", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData("assets\\app.js", false)]
    [InlineData(".env", false)]
    [InlineData("a//b.js", false)]
    [InlineData("C:x.js", false)]
    public void Paths_stay_inside_the_package(string path, bool safe) => Assert.Equal(safe, PackageRules.IsSafePath(path));

    [Fact]
    public void Only_static_files_are_allowed()
    {
        var problems = PackageRules.Check([new PackageFile("index.html", 10), new PackageFile("run.sh", 10)], Manifest);

        Assert.Contains(problems, p => p.Contains("run.sh", StringComparison.Ordinal));
    }

    [Fact]
    public void The_entry_must_be_in_the_package()
    {
        var problems = PackageRules.Check([new PackageFile("app.js", 10)], Manifest);

        Assert.Contains(problems, p => p.Contains("'index.html' is named in homemix.json", StringComparison.Ordinal));
    }

    [Fact]
    public void Too_much_unpacked_is_refused()
    {
        var problems = PackageRules.Check([new PackageFile("index.html", PackageRules.MaxUnpackedBytes + 1)], Manifest);

        Assert.NotEmpty(problems);
    }

    [Fact]
    public void A_package_is_its_manifest_and_files()
    {
        var package = AddonPackage.Create(
            new string('a', 64), Manifests.Valid(), [new PackageFile("homemix.json", 100), new PackageFile("index.html", 10)],
            1024, "https://github.com/someone/pomodoro", "v1.0.0", Guid.NewGuid(), DateTimeOffset.UnixEpoch);

        Assert.True(package.IsSuccess);
        Assert.Equal("pomodoro", package.Value.AddonId);
        Assert.Equal("1.0.0", package.Value.Version);
    }
}
