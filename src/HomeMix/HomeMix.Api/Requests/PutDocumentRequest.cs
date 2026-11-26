using System.Text.Json;

namespace HomeMix.Api.Requests;

/// <param name="ExpiresAt">When the document expires and is no longer returned; without it, never.</param>
/// <param name="Version">The version read before; without it the write wins.</param>
public sealed record PutDocumentRequest(JsonElement Value, int? Version, DateTimeOffset? ExpiresAt = null);
