using System.Text.Json;
using System.Text.Json.Serialization;

namespace MacDeviceEmulator.Configuration;

public sealed class PairedDeviceState
{
    public Guid DeviceId { get; set; }
    public Guid HouseholdId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}

public sealed class EmulatorStorage
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public Dictionary<string, PairedDeviceState> Devices { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<EmulatorStorage> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return new EmulatorStorage();
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<EmulatorStorage>(json) ?? new EmulatorStorage();
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"[Storage] ERROR: Corrupt configuration file at '{path}'. Error: {ex.Message}");
            throw new InvalidOperationException($"Corrupt configuration file at '{path}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Storage] ERROR: Failed reading configuration file at '{path}'. Error: {ex.Message}");
            throw;
        }
    }

    public async Task<PairedDeviceState?> GetDeviceAsync(string key)
    {
        await _lock.WaitAsync();
        try
        {
            return Devices.TryGetValue(key, out var s) ? s : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public bool TryGetDevice(string key, out PairedDeviceState? state)
    {
        lock (Devices)
        {
            return Devices.TryGetValue(key, out state);
        }
    }

    public async Task SaveDeviceAsync(string key, PairedDeviceState state, string path, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            lock (Devices)
            {
                Devices[key] = state;
            }
            await SaveSnapshotAsync(path, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveDeviceAsync(string key, string path, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            lock (Devices)
            {
                Devices.Remove(key);
            }
            await SaveSnapshotAsync(path, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await SaveSnapshotAsync(path, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveSnapshotAsync(string path, CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        Dictionary<string, PairedDeviceState> snapshot;
        lock (Devices)
        {
            snapshot = new Dictionary<string, PairedDeviceState>(Devices, StringComparer.OrdinalIgnoreCase);
        }

        var storageSnapshot = new EmulatorStorage { Devices = snapshot };
        var json = JsonSerializer.Serialize(storageSnapshot, new JsonSerializerOptions { WriteIndented = true });

        var tempPath = Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, $".tmp_{Guid.NewGuid():N}.json");

        try
        {
            var fileOptions = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous
            };

            if (!OperatingSystem.IsWindows())
            {
                fileOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            await using (var stream = new FileStream(tempPath, fileOptions))
            await using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(json.AsMemory(), cancellationToken);
                await writer.FlushAsync(cancellationToken);
            }

            if (!OperatingSystem.IsWindows())
            {
                try
                {
                    File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
                catch
                {
                    // Best effort if filesystem doesn't support unix mode
                }
            }

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }
}
