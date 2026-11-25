using Accounts.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Accounts.Infrastructure.Persistence;

/// <summary>2.15: <c>{Avatars:Path}/{userId}.bin</c>; the path is the <c>homeapps_avatars</c> volume, <c>/var/data/avatars</c> by default.</summary>
internal sealed class FileAvatarStore(IConfiguration configuration) : IAvatarStore
{
    private readonly string _root = configuration["Avatars:Path"] ?? "/var/data/avatars";

    public async Task SaveAsync(Guid userId, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);
        var target = PathOf(userId);
        var temp = $"{target}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temp, content, cancellationToken);
        File.Move(temp, target, overwrite: true);
    }

    public async Task<byte[]?> ReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var path = PathOf(userId);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
    }

    private string PathOf(Guid userId) => Path.Combine(_root, $"{userId}.bin");
}
