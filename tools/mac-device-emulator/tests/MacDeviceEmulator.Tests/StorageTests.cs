using MacDeviceEmulator.Configuration;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class StorageTests
{
    [Fact]
    public async Task Storage_saves_and_loads_correctly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"emulator-storage-test-{Guid.NewGuid():N}.json");
        try
        {
            var storage = new EmulatorStorage();
            storage.Devices["satellite"] = new PairedDeviceState
            {
                DeviceId = Guid.NewGuid(),
                HouseholdId = Guid.NewGuid(),
                Name = "Mac Satellite (test)",
                Secret = "secret123",
                Slug = "mac-satellite-test",
                Purpose = "assistant"
            };

            await storage.SaveAsync(tempFile);

            var loaded = await EmulatorStorage.LoadAsync(tempFile);
            Assert.True(loaded.Devices.ContainsKey("satellite"));
            var dev = loaded.Devices["satellite"];
            Assert.Equal("Mac Satellite (test)", dev.Name);
            Assert.Equal("secret123", dev.Secret);
            Assert.Equal("assistant", dev.Purpose);

            if (!OperatingSystem.IsWindows())
            {
                var mode = File.GetUnixFileMode(tempFile);
                Assert.True(mode.HasFlag(UnixFileMode.UserRead));
                Assert.True(mode.HasFlag(UnixFileMode.UserWrite));
                Assert.False(mode.HasFlag(UnixFileMode.GroupRead));
                Assert.False(mode.HasFlag(UnixFileMode.OtherRead));
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
