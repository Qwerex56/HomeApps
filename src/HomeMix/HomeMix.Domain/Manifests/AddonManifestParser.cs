using System.Text.Json;
using FluentResults;

namespace HomeMix.Domain.Manifests;

/// <summary>
/// Reads <c>homemix.json</c> and reports every problem at once (one <see cref="ManifestErrors.Invalid"/> each), like
/// the plugin manifest validator. The rules are docs/homemix.md, "The package".
/// </summary>
public static class AddonManifestParser
{
    public const int SupportedVersion = 1;
    public const int MaxIdLength = 31;

    public static Result<AddonManifest> Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException exception)
        {
            return Result.Fail(ManifestErrors.Invalid($"homemix.json is not valid JSON: {exception.Message}"));
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Result.Fail(ManifestErrors.Invalid("homemix.json must be an object."));
            }

            return new ManifestReader(document.RootElement).Read();
        }
    }
}
