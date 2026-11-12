namespace Jarvis.Application.Gateway;

/// <summary>
/// A token for the sources that acts for the caller: Auth's 5-minute delegated token (<c>act</c>), so every source
/// treats the call as an assistant's (JV3, JV13) whoever connected to the gateway.
/// </summary>
public interface IDelegation
{
    /// <param name="client">Who connected: the OAuth client id, or <c>direct</c> for a person's own token.</param>
    Task<string> TokenForAsync(Guid userId, string client, CancellationToken cancellationToken);
}
