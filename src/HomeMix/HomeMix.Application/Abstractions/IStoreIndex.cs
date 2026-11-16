namespace HomeMix.Application.Abstractions;

/// <summary>
/// <c>addons.json</c> (homemix-2.md S6): the quick listing of official add-ons, repositories the HomeApps team keeps.
/// They become official store entries; an unreadable index lists nothing new and changes nothing.
/// </summary>
public interface IStoreIndex
{
    Task<IReadOnlyList<string>> RepositoriesAsync(CancellationToken cancellationToken);
}
