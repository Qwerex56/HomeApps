using System.Text.Json;
using HomeMix.Domain.Data;

namespace HomeMix.Application.Data;

public sealed record DocumentView(string Key, JsonElement Value, int Version, Guid UpdatedBy, DateTimeOffset UpdatedAt)
{
    public static DocumentView From(AddonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        using var json = JsonDocument.Parse(document.Value);
        return new DocumentView(document.Key, json.RootElement.Clone(), document.Version, document.UpdatedBy, document.UpdatedAt);
    }
}
