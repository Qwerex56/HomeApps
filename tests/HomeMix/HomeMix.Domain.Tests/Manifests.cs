namespace HomeMix.Domain.Tests;

/// <summary>homemix.json texts for the tests: <see cref="Valid"/> with any field replaced.</summary>
internal static class Manifests
{
    public static string Valid(
        string id = "pomodoro",
        string version = "1.0.0",
        string requires = "2.3.0",
        string? targets = null,
        string capabilities = """["storage.household"]""",
        string network = "[]",
        string? extra = null) =>
        $$"""
        {
          "homemixVersion": 1,
          "id": "{{id}}",
          "version": "{{version}}",
          "requires": { "homeApps": "{{requires}}" },
          "name": { "pl": "Pomodoro", "en": "Pomodoro" },
          "icon": "pi-stopwatch",
          {{(targets is null ? string.Empty : $"\"targets\": {targets},")}}
          "entry": "index.html",
          "capabilities": {{capabilities}},
          "network": {{network}}{{(extra is null ? string.Empty : "," + extra)}}
        }
        """;
}
