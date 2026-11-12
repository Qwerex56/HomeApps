using System.Text.Json.Serialization;

namespace HomeMix.Infrastructure.Store;

/// <summary><c>addons.json</c>: <c>{ "addons": [ { "repository": "https://github.com/owner/repo" } ] }</c>.</summary>
internal sealed record StoreIndexFile([property: JsonPropertyName("addons")] IReadOnlyList<StoreIndexEntry> Addons);
