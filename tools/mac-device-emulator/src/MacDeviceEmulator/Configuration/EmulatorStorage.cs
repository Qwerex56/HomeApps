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
        catch
        {
            return new EmulatorStorage();
        }
    }

    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, cancellationToken);

        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch
            {
                // Best effort on file systems that may not support unix permissions
            }
        }
    }
}
