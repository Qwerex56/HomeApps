namespace Jarvis.VoiceBridge;

/// <summary>Who the bridge speaks as: the person (<see cref="OAuthLogin"/>) or, once paired, its household (<see cref="SatelliteLogin"/>).</summary>
internal interface IAccessToken
{
    /// <summary>"computer" for a person's Mac, "satellite" for a household's (the hello on <c>/devices/link</c>).</summary>
    string Kind { get; }

    Task<string> AccessTokenAsync(CancellationToken cancellationToken);

    /// <summary>Forgets what this computer keeps for it.</summary>
    void Forget();
}
