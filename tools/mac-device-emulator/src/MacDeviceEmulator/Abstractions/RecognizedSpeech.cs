namespace MacDeviceEmulator.Abstractions;

public sealed record RecognizedSpeech(string Text, DateTimeOffset Timestamp);
