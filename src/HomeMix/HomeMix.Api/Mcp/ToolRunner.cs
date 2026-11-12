using System.Globalization;
using System.Text;
using System.Text.Json;
using FluentResults;
using HomeMix.Application.Data;
using HomeMix.Application.Data.DeleteDocument;
using HomeMix.Application.Data.GetDocument;
using HomeMix.Application.Data.ListDocuments;
using HomeMix.Application.Data.PutDocument;
using HomeMix.Domain.Data;
using HomeMix.Domain.Tools;
using MediatR;

namespace HomeMix.Api.Mcp;

/// <summary>
/// Runs a declared tool through the same data use cases as the bridge, so roles, limits and errors are those of HTTP,
/// and answers in plain text: the tool's <c>answer</c> with <c>{field}</c> and <c>{key}</c> filled in, or a short
/// default. Entries are listed newest first, at most <see cref="MaxListed"/>.
/// </summary>
internal sealed class ToolRunner(ToolCatalog catalog, ISender sender, TimeProvider time)
{
    public const int MaxListed = 30;
    private const int MaxAnswer = 4000;

    public async Task<Result<string>> CallAsync(
        string name, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        var resolved = await catalog.ResolveAsync(name, arguments, cancellationToken);
        if (resolved.IsFailed)
        {
            return resolved.ToResult();
        }

        var call = resolved.Value;
        var id = call.Place.Installation.Id;
        if (call.Tool is null)
        {
            var summary = call.Place.Manifest.Summary!;
            return (await sender.Send(new ListDocumentsQuery(id, summary.Scope, summary.Collection), cancellationToken))
                .Map(docs => Listing($"{call.Place.Manifest.Name.En} in {call.Place.Name}", summary.Collection, docs));
        }

        var tool = call.Tool;
        switch (tool.Action)
        {
            case ToolAction.List:
                return (await sender.Send(new ListDocumentsQuery(id, tool.Scope, tool.Collection), cancellationToken))
                    .Map(docs => tool.Answer is null ? Listing(null, tool.Collection, docs) : Fill(tool.Answer, null, call.Fields));
            case ToolAction.Get:
                return (await sender.Send(new GetDocumentQuery(id, tool.Scope, tool.Collection, call.Key!), cancellationToken))
                    .Map(doc => tool.Answer is null ? Line(doc) : Fill(tool.Answer, doc.Key, Fields(doc.Value)));
            case ToolAction.Add:
            {
                var key = NewKey();
                var value = JsonSerializer.SerializeToElement(call.Fields);
                return (await sender.Send(new PutDocumentCommand(id, tool.Scope, tool.Collection, key, value, null), cancellationToken))
                    .Map(doc => Fill(tool.Answer ?? "Added {key}.", doc.Key, call.Fields));
            }

            case ToolAction.Update:
            {
                var current = await sender.Send(new GetDocumentQuery(id, tool.Scope, tool.Collection, call.Key!), cancellationToken);
                if (current.IsFailed)
                {
                    return current.ToResult();
                }

                var merged = Fields(current.Value.Value);
                foreach (var (field, value) in call.Fields)
                {
                    merged[field] = value;
                }

                var put = new PutDocumentCommand(
                    id, tool.Scope, tool.Collection, call.Key!, JsonSerializer.SerializeToElement(merged), current.Value.Version);
                return (await sender.Send(put, cancellationToken)).Map(doc => Fill(tool.Answer ?? "Updated {key}.", doc.Key, merged));
            }

            case ToolAction.Delete:
            {
                var deleted = await sender.Send(new DeleteDocumentCommand(id, tool.Scope, tool.Collection, call.Key!, null), cancellationToken);
                return deleted.IsFailed ? deleted : Result.Ok(Fill(tool.Answer ?? "Deleted {key}.", call.Key, call.Fields));
            }

            default:
                throw new InvalidOperationException($"Unknown action {tool.Action}.");
        }
    }

    /// <summary>A short time-ordered key, so entries added by JARVIS sort by when they were added.</summary>
    private string NewKey() =>
        string.Create(CultureInfo.InvariantCulture, $"{time.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}");

    private static string Listing(string? title, string collection, IReadOnlyList<DocumentView> documents)
    {
        var text = new StringBuilder();
        if (title is not null)
        {
            text.Append(title).Append(": ");
        }

        if (documents.Count == 0)
        {
            return text.Append(CultureInfo.InvariantCulture, $"nothing in {collection} yet.").ToString();
        }

        text.Append(CultureInfo.InvariantCulture, $"{documents.Count} in {collection}");
        text.Append(documents.Count > MaxListed ? string.Create(CultureInfo.InvariantCulture, $", the newest {MaxListed}:") : ":");
        foreach (var document in documents.OrderByDescending(d => d.UpdatedAt).Take(MaxListed))
        {
            text.AppendLine().Append("- ").Append(Line(document));
        }

        return Cut(text.ToString());
    }

    private static string Line(DocumentView document) => $"{document.Key}: {Describe(document.Value)}";

    /// <summary>An object as "task: Raport, minutes: 25"; anything else as its plain value.</summary>
    private static string Describe(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => string.Join(", ", value.EnumerateObject().Select(p => $"{p.Name}: {Plain(p.Value)}")),
        _ => Plain(value),
    };

    private static string Plain(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!,
        JsonValueKind.Null => "none",
        JsonValueKind.True => "yes",
        JsonValueKind.False => "no",
        _ => value.GetRawText(),
    };

    private static Dictionary<string, JsonElement> Fields(JsonElement value) =>
        value.ValueKind == JsonValueKind.Object
            ? value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal)
            : new Dictionary<string, JsonElement>(StringComparer.Ordinal);

    private static string Fill(string template, string? key, IReadOnlyDictionary<string, JsonElement> fields)
    {
        var text = template.Replace("{key}", key ?? string.Empty, StringComparison.Ordinal);
        foreach (var (name, value) in fields)
        {
            text = text.Replace($"{{{name}}}", Plain(value), StringComparison.Ordinal);
        }

        return Cut(text);
    }

    private static string Cut(string text) => text.Length <= MaxAnswer ? text : string.Concat(text.AsSpan(0, MaxAnswer), "…");
}
