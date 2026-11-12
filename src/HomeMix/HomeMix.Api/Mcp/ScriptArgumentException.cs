namespace HomeMix.Api.Mcp;

/// <summary>A script passed <c>ctx</c> something it does not take; it gets a <c>homemix.bad_request</c> error back.</summary>
internal sealed class ScriptArgumentException(string message) : Exception(message);
