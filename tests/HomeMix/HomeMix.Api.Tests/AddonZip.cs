using System.IO.Compression;
using System.Text;

namespace HomeMix.Api.Tests;

/// <summary>Builds add-on zips for the tests.</summary>
internal static class AddonZip
{
    public const string Index = "<!doctype html><title>Pomodoro</title><script type=\"module\" src=\"app.js\"></script>";

    public static string Manifest(string id = "pomodoro", string version = "1.0.0", string capabilities = """["storage.household"]""",
        string network = "[]", string? targets = null, string? extra = null) =>
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
          "network": {{network}}{{(extra is null ? string.Empty : "," + extra)}}
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

    /// <summary>An add-on with declared tools and a summary for JARVIS (docs/homemix.md, "JARVIS").</summary>
    public static byte[] Notes(string version = "1.0.0") => Create(Manifest(id: "notes", version: version,
        capabilities: """["storage.household", "storage.user"]""", extra: """
          "jarvis": true,
          "permissions": [{ "id": "notes.write", "minRole": "Member" }, { "id": "vault", "minRole": "Admin" }],
          "tools": [
            { "name": "add_note", "description": "Adds a note.", "collection": "notes", "action": "add",
              "fields": { "text": { "type": "string", "required": true }, "stars": { "type": "integer", "default": 1 } },
              "answer": "Noted: {text}." },
            { "name": "notes", "description": "Lists the notes.", "collection": "notes", "action": "list" },
            { "name": "star", "description": "Changes a note's stars.", "collection": "notes", "action": "update",
              "fields": { "stars": { "type": "integer" } } },
            { "name": "forget", "description": "Deletes a note.", "collection": "notes", "action": "delete" },
            { "name": "secret", "description": "Lists the vault.", "collection": "vault", "action": "list", "permission": "vault" }
          ],
          "summary": { "collection": "notes" }
        """));

    /// <summary>An add-on with script tools (A7), each <c>tools/{name}.js</c> given as (name, source).</summary>
    public static byte[] Scripts(string version, string tools, params (string Name, string Source)[] scripts) => Create(
        Manifest(id: "calc", version: version, capabilities: """["storage.household", "household.members"]""",
            network: """["https://api.example.com"]""", extra: $$"""
              "jarvis": true,
              "permissions": [{ "id": "admin", "minRole": "Admin" }],
              "tools": {{tools}}
            """),
        [("index.html", Index), .. scripts.Select(s => ($"tools/{s.Name}.js", s.Source))]);

    private static void Add(ZipArchive zip, string path, string content)
    {
        using var stream = zip.CreateEntry(path).Open();
        stream.Write(Encoding.UTF8.GetBytes(content));
    }
}
