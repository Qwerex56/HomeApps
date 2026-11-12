using System.Text.Json;

namespace HomeMix.Api.Requests;

/// <param name="Version">The version read before; without it the write wins.</param>
public sealed record PutDocumentRequest(JsonElement Value, int? Version);
