namespace MacDeviceEmulator.Configuration;

public sealed record EmulatorOptions
{
    public string ServerUrl { get; init; } = "http://localhost:5000";
    public string ConfigPath { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".homeapps", "mac-devices.json");
    public string? PairingCodeSatellite { get; init; }
    public string? PairingCodePowered { get; init; }
    public string? PairingCodeBattery { get; init; }
    public string? ComputerUserToken { get; init; }
    public bool RunSatellite { get; init; } = true;
    public bool RunPowered { get; init; } = true;
    public bool RunBattery { get; init; } = true;
    public bool RunComputer { get; init; } = true;
    public bool EnableContinuousListening { get; init; } = true;
    public bool StartMuted { get; init; }
    public string? SttCommand { get; init; }
    public IReadOnlyList<string> SttArguments { get; init; } = [];
    public IReadOnlyList<string>? WakeWords { get; init; }
}
