using System.Security.Cryptography;
using System.Text;

namespace HomeMix.Domain.Store;

/// <summary>
/// The key of a store entry (homemix-2.md S3): the add-on id is not unique in the store, so an entry is
/// <c>{id}-{8 hex of SHA-256(submitter + repository)}</c>, and an official one <c>{id}-official</c>. The same person
/// with the same repository always gets the same entry; a new release is a new version of it, not a new entry.
/// </summary>
public static class ListingKey
{
    public const int MaxLength = 31 + 1 + 8;

    public static string For(string addonId, Guid submittedBy, string repository)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{submittedBy:D}|{repository.ToLowerInvariant()}"));
        return $"{addonId}-{Convert.ToHexStringLower(hash)[..8]}";
    }

    public static string Official(string addonId) => $"{addonId}-official";

    /// <summary>The source of a built-in add-on in the store's numbers (it has no repository).</summary>
    public const string OfficialSource = "official";
}
