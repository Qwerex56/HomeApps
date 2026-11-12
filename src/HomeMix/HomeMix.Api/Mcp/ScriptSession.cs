using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Data.DeleteDocument;
using HomeMix.Application.Data.GetDocument;
using HomeMix.Application.Data.ListDocuments;
using HomeMix.Application.Data.PutDocument;
using HomeMix.Domain.Data;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;
using MediatR;

namespace HomeMix.Api.Mcp;

/// <summary>
/// One run's <c>ctx</c> calls. Jint calls it synchronously, so each one waits for its use case; the run's thread is
/// its own (<see cref="ScriptRunner"/>). Answers are JSON: <c>{"value": …}</c> or <c>{"error": {"code", "message"}}</c>.
/// </summary>
internal sealed class ScriptSession(
    ToolCall call,
    ISender sender,
    IHouseholdRoles households,
    ICurrentUser currentUser,
    HttpClient http,
    CancellationToken cancellationToken)
{
    private int _calls;
    private int _fetches;

    public string Call(string method, string argumentsJson)
    {
        try
        {
            if (++_calls > ScriptLimits.HostCalls)
            {
                return Error("homemix.script_limit", $"At most {ScriptLimits.HostCalls} ctx calls in one run.");
            }

            using var parsed = JsonDocument.Parse(argumentsJson);
            var args = parsed.RootElement.EnumerateArray().Select(a => a.Clone()).ToList();
            return method switch
            {
                "data.list" => Answer(sender.Send(new ListDocumentsQuery(Id, Scope(args, 1), Text(args, 0)), cancellationToken).GetAwaiter().GetResult()),
                "data.get" => Get(args),
                "data.put" => ReadOnlyRefusal() ?? Answer(sender.Send(new PutDocumentCommand(
                    Id, Scope(Option(args, 3, "scope")), Text(args, 0), Text(args, 1), Arg(args, 2), Version(args, 3)), cancellationToken).GetAwaiter().GetResult()),
                "data.delete" => ReadOnlyRefusal() ?? Answer(sender.Send(new DeleteDocumentCommand(
                    Id, Scope(Option(args, 2, "scope")), Text(args, 0), Text(args, 1), Version(args, 2)), cancellationToken).GetAwaiter().GetResult()),
                "members" => Members(),
                "fetch" => Fetch(Text(args, 0), Arg(args, 1)),
                _ => Error("homemix.unknown_method", $"ctx has no {method}."),
            };
        }
        catch (ScriptArgumentException e)
        {
            return Error("homemix.bad_request", e.Message);
        }
    }

    private Guid Id => call.Place.Installation.Id;

    private string Get(List<JsonElement> args)
    {
        var found = sender.Send(new GetDocumentQuery(Id, Scope(args, 2), Text(args, 0), Text(args, 1)), cancellationToken).GetAwaiter().GetResult();
        return found.IsFailed && found.Errors.Any(e => Code(e) == "homemix.document_not_found") ? Value(null) : Answer(found);
    }

    private string? ReadOnlyRefusal() => call.Tool!.ReadOnly
        ? Error("homemix.read_only_tool", "This tool is readOnly in homemix.json: it may not change data.")
        : null;

    private string Members()
    {
        var installation = call.Place.Installation;
        if (!installation.Capabilities.Contains(AddonCapabilities.HouseholdMembers))
        {
            return Error("homemix.capability_missing", $"The add-on did not declare {AddonCapabilities.HouseholdMembers}.");
        }

        if (installation.TargetKind == TargetKind.User)
        {
            return Value(new[] { new { id = currentUser.RequiredUserId, name = string.Empty, avatarUrl = (string?)null, role = nameof(HouseholdRole.Owner) } });
        }

        var members = households.GetMembersAsync(installation.TargetId, cancellationToken).GetAwaiter().GetResult();
        return Value(members.Select(m => new { id = m.Id, name = m.Name, avatarUrl = m.AvatarUrl, role = m.Role.ToString() }));
    }

    /// <summary>Only to the add-on's <c>network</c> origins, GET or POST, no redirects, at most <see cref="ScriptLimits.FetchBytes"/>.</summary>
    private string Fetch(string url, JsonElement init)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !Allowed(call.Place.Installation.Network, uri))
        {
            return Error("homemix.network_denied", $"{url} is not in the add-on's network.");
        }

        if (++_fetches > ScriptLimits.Fetches)
        {
            return Error("homemix.script_limit", $"At most {ScriptLimits.Fetches} fetches in one run.");
        }

        var method = init.ValueKind == JsonValueKind.Object && init.TryGetProperty("method", out var m) && m.ValueKind == JsonValueKind.String
            ? m.GetString()!.ToUpperInvariant()
            : "GET";
        if (method is not ("GET" or "POST"))
        {
            return Error("homemix.bad_request", "fetch takes GET or POST.");
        }

        using var request = new HttpRequestMessage(new HttpMethod(method), uri);
        if (method == "POST" && init.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.String)
        {
            var type = init.TryGetProperty("headers", out var headers) && headers.ValueKind == JsonValueKind.Object
                && headers.TryGetProperty("content-type", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
            request.Content = new StringContent(body.GetString()!, Encoding.UTF8, MediaTypeHeaderValue.TryParse(type, out var media) ? media.MediaType! : "application/json");
        }

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(ScriptLimits.FetchTime);
        try
        {
            using var response = http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, limit.Token).GetAwaiter().GetResult();
            using var stream = response.Content.ReadAsStream(limit.Token);
            var buffer = new byte[ScriptLimits.FetchBytes + 1];
            var read = 0;
            int chunk;
            while (read < buffer.Length && (chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
            {
                read += chunk;
            }

            if (read > ScriptLimits.FetchBytes)
            {
                return Error("homemix.script_limit", $"The answer from {uri.Host} is larger than {ScriptLimits.FetchBytes / 1024} KB.");
            }

            return Value(new
            {
                status = (int)response.StatusCode,
                headers = new Dictionary<string, string?> { ["content-type"] = response.Content.Headers.ContentType?.ToString() },
                body = Encoding.UTF8.GetString(buffer, 0, read),
            });
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return Error("homemix.fetch_failed", $"{uri.Host} did not answer: {e.Message}");
        }
    }

    private static bool Allowed(IReadOnlyList<string> network, Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) && network.Any(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var allowed)
            && Uri.Compare(allowed, uri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0);

    private static string Answer<T>(Result<T> result) => result.IsSuccess ? Value(result.Value) : Failure(result.Errors);

    private static string Answer(Result result) => result.IsSuccess ? Value(null) : Failure(result.Errors);

    private static string Failure(IReadOnlyList<IError> errors) => Error(Code(errors[0]) ?? "homemix.error", errors[0].Message);

    private static string? Code(IError error) => error.Metadata.TryGetValue(AppError.CodeKey, out var code) ? code as string : null;

    private static string Value(object? value) => JsonSerializer.Serialize(new { value }, ScriptBridge.Json);

    private static string Error(string code, string message) => JsonSerializer.Serialize(new { error = new { code, message } }, ScriptBridge.Json);

    private static JsonElement Arg(List<JsonElement> args, int index) => index < args.Count ? args[index] : default;

    private static string Text(List<JsonElement> args, int index) =>
        Arg(args, index) is { ValueKind: JsonValueKind.String } text
            ? text.GetString()!
            : throw new ScriptArgumentException($"Argument {index + 1} must be a string.");

    private static JsonElement Option(List<JsonElement> args, int index, string name) =>
        Arg(args, index) is { ValueKind: JsonValueKind.Object } options && options.TryGetProperty(name, out var value) ? value : default;

    private static DataScope Scope(List<JsonElement> args, int index) => Scope(Arg(args, index));

    private static DataScope Scope(JsonElement scope) => scope.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => DataScope.Household,
        JsonValueKind.String when scope.GetString() == "household" => DataScope.Household,
        JsonValueKind.String when scope.GetString() == "user" => DataScope.User,
        _ => throw new ScriptArgumentException("scope must be household or user."),
    };

    private static int? Version(List<JsonElement> args, int index) =>
        Option(args, index, "version") is { ValueKind: JsonValueKind.Number } version && version.TryGetInt32(out var v) ? v : null;
}
