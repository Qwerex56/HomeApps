using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Jarvis.VoiceBridge;

/// <summary>
/// "Hey Jarvis" the way openWakeWord does it, on this computer: every 80 ms of 16 kHz audio becomes mel frames, the
/// last 76 frames one speech embedding, and the last 16 embeddings the model's score from 0 to 1. Nothing leaves the
/// Mac until the score passes the threshold (jarvis-voice-bridge.md, V3).
/// </summary>
internal sealed class WakeWord : IDisposable
{
    public const int FrameSamples = 1280;

    private const int Context = 480;
    private const int MelWindow = 76;
    private const int MelBins = 32;
    private const int Embeddings = 16;
    private const int EmbeddingSize = 96;

    /// <summary>The first scores after a reset come from the warm-up noise, not from speech.</summary>
    private const int WarmUpFrames = 5;

    private readonly InferenceSession _mel;
    private readonly InferenceSession _embedding;
    private readonly InferenceSession _jarvis;
    private readonly List<float> _raw = [];
    private readonly List<float[]> _melFrames = [];
    private readonly List<float[]> _features = [];
    private int _sinceReset;

    public WakeWord(string models)
    {
        using var options = new SessionOptions { IntraOpNumThreads = 1, InterOpNumThreads = 1 };
        _mel = new InferenceSession(Path.Combine(models, "melspectrogram.onnx"), options);
        _embedding = new InferenceSession(Path.Combine(models, "embedding_model.onnx"), options);
        _jarvis = new InferenceSession(Path.Combine(models, "hey_jarvis_v0.1.onnx"), options);
        Reset();
    }

    /// <summary>Forgets what was heard, e.g. after JARVIS spoke, so his own voice cannot wake him.</summary>
    public void Reset()
    {
        _raw.Clear();
        _melFrames.Clear();
        _features.Clear();
        for (var i = 0; i < MelWindow; i++)
        {
            _melFrames.Add(Enumerable.Repeat(1f, MelBins).ToArray());
        }

        // Like openWakeWord: the embedding history starts from 4 s of quiet noise.
        var random = new Random(7);
        for (var frame = 0; frame < 50; frame++)
        {
            Push(Enumerable.Range(0, FrameSamples).Select(_ => (short)random.Next(-1000, 1000)).ToArray());
        }

        _sinceReset = 0;
    }

    /// <param name="frame"><see cref="FrameSamples"/> samples of 16 kHz 16-bit mono audio.</param>
    /// <returns>How sure the model is that "Hey Jarvis" was just said, 0 to 1.</returns>
    public float Process(short[] frame)
    {
        Push(frame);
        var input = new DenseTensor<float>(new[] { 1, Embeddings, EmbeddingSize });
        var last = _features.Skip(_features.Count - Embeddings).SelectMany(f => f).ToArray();
        last.CopyTo(input.Buffer.Span);
        var score = Run(_jarvis, input)[0];
        return ++_sinceReset <= WarmUpFrames ? 0 : score;
    }

    /// <summary>The highest score anywhere in a recording (<c>--check-wake</c>).</summary>
    public float Scan(short[] pcm)
    {
        Reset();
        var best = 0f;
        for (var at = 0; at + FrameSamples <= pcm.Length; at += FrameSamples)
        {
            best = Math.Max(best, Process(pcm[at..(at + FrameSamples)]));
        }

        return best;
    }

    public void Dispose()
    {
        _mel.Dispose();
        _embedding.Dispose();
        _jarvis.Dispose();
    }

    private void Push(short[] frame)
    {
        _raw.AddRange(frame.Select(s => (float)s));
        if (_raw.Count > FrameSamples + Context)
        {
            _raw.RemoveRange(0, _raw.Count - FrameSamples - Context);
        }

        var audio = new DenseTensor<float>(_raw.ToArray(), new[] { 1, _raw.Count });
        var mel = Run(_mel, audio);
        for (var at = 0; at + MelBins <= mel.Length; at += MelBins)
        {
            _melFrames.Add(mel[at..(at + MelBins)].Select(v => (v / 10f) + 2f).ToArray());
        }

        if (_melFrames.Count > 10 * MelWindow)
        {
            _melFrames.RemoveRange(0, _melFrames.Count - MelWindow);
        }

        var window = new DenseTensor<float>(new[] { 1, MelWindow, MelBins, 1 });
        _melFrames.Skip(_melFrames.Count - MelWindow).SelectMany(f => f).ToArray().CopyTo(window.Buffer.Span);
        _features.Add(Run(_embedding, window));
        if (_features.Count > 10 * Embeddings)
        {
            _features.RemoveRange(0, _features.Count - Embeddings);
        }
    }

    private static float[] Run(InferenceSession session, DenseTensor<float> input)
    {
        using var results = session.Run([NamedOnnxValue.CreateFromTensor(session.InputMetadata.Keys.First(), input)]);
        return results[0].AsTensor<float>().ToArray();
    }
}
