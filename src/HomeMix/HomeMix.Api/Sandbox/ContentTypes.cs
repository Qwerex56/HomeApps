namespace HomeMix.Api.Sandbox;

/// <summary>Content types for the extensions a package may hold (PackageRules.AllowedExtensions).</summary>
internal static class ContentTypes
{
    private static readonly Dictionary<string, string> ByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".js"] = "text/javascript; charset=utf-8",
        [".mjs"] = "text/javascript; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".svg"] = "image/svg+xml",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".woff2"] = "font/woff2",
        [".md"] = "text/markdown; charset=utf-8",
    };

    public static string For(string path) =>
        ByExtension.TryGetValue(Path.GetExtension(path), out var type) ? type : "application/octet-stream";
}
