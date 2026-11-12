using System.Text.Json;

namespace HomeMix.Domain.Tools;

/// <summary>One argument of an <c>add</c> or <c>update</c> tool, stored as a field of the document's value.</summary>
/// <param name="Type"><c>string</c>, <c>integer</c>, <c>number</c> or <c>boolean</c>, as in JSON Schema.</param>
/// <param name="Default">Used by <c>add</c> when the argument is missing.</param>
public sealed record AddonToolField(string Name, string Type, string? Description, bool Required, JsonElement? Default);
