namespace Jarvis.Application.Gateway;

/// <summary>
/// A token for the sources that acts for the caller: Auth's 5-minute delegated token (<c>act</c>), so every source
/// treats the call as an assistant's (JV3, JV13) whoever connected to the gateway.
/// </summary>
public interface IDelegation
{
    /// <param name="channel"><c>mcp</c> for a client of the gateway, otherwise the run's channel (<c>app</c>, <c>chat</c>).</param>
    /// <param name="reference">Who or what: the OAuth client id, <c>direct</c>, or <c>run:{id}</c>; in Auth's log.</param>
    Task<string> TokenForAsync(Guid userId, string channel, string reference, CancellationToken cancellationToken);
}
