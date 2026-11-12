using System.IO.Compression;
using System.Text;

namespace HomeMix.Api.Tests;

/// <summary>Builds add-on zips for the tests.</summary>
internal static class AddonZip
{
    public const string Index = "<!doctype html><title>Pomodoro</title><script type=\"module\" src=\"app.js\"></script>";

    public static string Manifest(string id = "pomodoro", string version = "1.0.0", string capabilities = """["storage.household"]""",
        string network = "[]", string? targets = null) =>
        $$"""
        {
          "homemixVersion": 1,
          "id": "{{id}}",
          "version": "{{version}}",
          "requires": { "homeApps": "2.3.0" },
          "name": { "pl": "Pomodoro", "en": "Pomodoro" },
          "icon": "pi-stopwatch",
          {{(targets is null ? string.Empty : $"\"targets\": {targets},")}}
          "entry": "index.html",
          "capabilities": {{capabilities}},
          "network": {{network}}
        }
        """;

    public static byte[] Create(string manifest, params (string Path, string Content)[] files)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "homemix.json", manifest);
            foreach (var (path, content) in files.Length == 0 ? [("index.html", Index), ("app.js", "console.log('hi');")] : files)
            {
                Add(zip, path, content);
            }
        }

        return buffer.ToArray();
    }

    public static byte[] Pomodoro(string version = "1.0.0", string capabilities = """["storage.household"]""", string network = "[]") =>
        Create(Manifest(version: version, capabilities: capabilities, network: network));

    private static void Add(ZipArchive zip, string path, string content)
    {
        using var stream = zip.CreateEntry(path).Open();
        stream.Write(Encoding.UTF8.GetBytes(content));
    }
}
