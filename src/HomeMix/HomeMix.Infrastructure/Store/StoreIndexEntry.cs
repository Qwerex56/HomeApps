using System.Text.Json.Serialization;

namespace HomeMix.Infrastructure.Store;

internal sealed record StoreIndexEntry([property: JsonPropertyName("repository")] string Repository);
