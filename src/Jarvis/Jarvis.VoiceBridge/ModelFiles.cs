using System.Security.Cryptography;

namespace Jarvis.VoiceBridge;

/// <summary>
/// The three openWakeWord models (v0.5.1): mel spectrogram, speech embedding and "hey jarvis". Downloaded once into
/// <see cref="BridgeOptions.Home"/> and checked by hash. The "hey jarvis" model is CC BY-NC-SA 4.0: fine for a home.
/// </summary>
internal static class ModelFiles
{
    private const string Release = "https://github.com/dscripka/openWakeWord/releases/download/v0.5.1/";

    private static readonly (string Name, string Sha256)[] Files =
    [
        ("melspectrogram.onnx", "ba2b0e0f8b7b875369a2c89cb13360ff53bac436f2895cced9f479fa65eb176f"),
        ("embedding_model.onnx", "70d164290c1d095d1d4ee149bc5e00543250a7316b59f31d056cff7bd3075c1f"),
        ("hey_jarvis_v0.1.onnx", "94a13cfe60075b132f6a472e7e462e8123ee70861bc3fb58434a73712ee0d2cb"),
    ];

    /// <returns>The folder with the models.</returns>
    public static async Task<string> EnsureAsync(HttpClient http, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(BridgeOptions.Home, "models");
        Directory.CreateDirectory(folder);
        foreach (var (name, sha256) in Files)
        {
            var path = Path.Combine(folder, name);
            if (File.Exists(path) && Hash(await File.ReadAllBytesAsync(path, cancellationToken)) == sha256)
            {
                continue;
            }

            Console.WriteLine($"Pobieram model {name}…");
            var bytes = await http.GetByteArrayAsync(new Uri(Release + name), cancellationToken);
            if (Hash(bytes) != sha256)
            {
                throw new InvalidOperationException($"{name}: the downloaded file does not match its hash.");
            }

            await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        }

        return folder;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
