using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Configuration;
using MacDeviceEmulator.Devices;
using MacDeviceEmulator.Services;

namespace MacDeviceEmulator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("   HomeApps macOS Test Device Emulator (H-5)      ");
        Console.WriteLine("==================================================");

        var options = ParseArguments(args);

        Console.WriteLine($"Server:       {options.ServerUrl}");
        Console.WriteLine($"Config:       {options.ConfigPath}");
        Console.WriteLine($"Satellite:    {(options.RunSatellite ? "ENABLED" : "DISABLED")}");
        Console.WriteLine($"Powered:      {(options.RunPowered ? "ENABLED" : "DISABLED")}");
        Console.WriteLine($"Battery:      {(options.RunBattery ? "ENABLED" : "DISABLED")}");
        Console.WriteLine($"Computer:     {(options.RunComputer ? "ENABLED" : "DISABLED")}");
        Console.WriteLine("--------------------------------------------------");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nStopping all emulated devices...");
            cts.Cancel();
        };

        var storage = await EmulatorStorage.LoadAsync(options.ConfigPath, cts.Token);
        var httpClient = new HttpClient();
        var homeAppsClient = new HomeAppsClient(httpClient);
        var processRunner = new ProcessRunner();
        var macSystem = new MacSystem(processRunner);
        var timeProvider = TimeProvider.System;

        var runningTasks = new List<Task>();

        if (options.RunSatellite)
        {
            var satellite = new SatelliteEmulatorDevice(
                options.ServerUrl,
                storage,
                options.ConfigPath,
                options.PairingCodeSatellite,
                homeAppsClient,
                () => new DeviceWebSocket(),
                macSystem,
                timeProvider);
            runningTasks.Add(Task.Run(() => satellite.RunAsync(cts.Token)));
        }

        if (options.RunPowered)
        {
            var powered = new PoweredEmulatorDevice(
                options.ServerUrl,
                storage,
                options.ConfigPath,
                options.PairingCodePowered,
                homeAppsClient,
                () => new DeviceWebSocket(),
                macSystem,
                timeProvider);
            runningTasks.Add(Task.Run(() => powered.RunAsync(cts.Token)));
        }

        if (options.RunBattery)
        {
            var battery = new BatteryEmulatorDevice(
                options.ServerUrl,
                storage,
                options.ConfigPath,
                options.PairingCodeBattery,
                homeAppsClient,
                macSystem);
            runningTasks.Add(Task.Run(() => battery.RunAsync(cts.Token)));
        }

        if (options.RunComputer)
        {
            var computer = new ComputerEmulatorDevice(
                options.ServerUrl,
                options.ComputerUserToken,
                () => new DeviceWebSocket(),
                macSystem);
            runningTasks.Add(Task.Run(() => computer.RunAsync(cts.Token)));
        }

        if (runningTasks.Count == 0)
        {
            Console.WriteLine("No devices selected to run. Exiting.");
            return 0;
        }

        try
        {
            await Task.WhenAll(runningTasks);
        }
        catch (OperationCanceledException)
        {
            // Clean exit
        }

        Console.WriteLine("Emulator finished.");
        return 0;
    }

    private static EmulatorOptions ParseArguments(string[] args)
    {
        var server = "https://app.localhost";
        var config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".homeapps", "mac-devices.json");
        string? pairSatellite = null;
        string? pairPowered = null;
        string? pairBattery = null;
        string? userToken = Environment.GetEnvironmentVariable("HOMEAPPS_USER_TOKEN");

        bool runSat = true;
        bool runPow = true;
        bool runBat = true;
        bool runComp = true;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--server", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                server = args[++i];
            }
            else if (arg.Equals("--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                config = args[++i];
            }
            else if (arg.Equals("--pair-satellite", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                pairSatellite = args[++i];
            }
            else if (arg.Equals("--pair-powered", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                pairPowered = args[++i];
            }
            else if (arg.Equals("--pair-battery", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                pairBattery = args[++i];
            }
            else if (arg.Equals("--user-token", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                userToken = args[++i];
            }
            else if (arg.Equals("--only-satellite", StringComparison.OrdinalIgnoreCase))
            {
                runSat = true; runPow = false; runBat = false; runComp = false;
            }
            else if (arg.Equals("--only-powered", StringComparison.OrdinalIgnoreCase))
            {
                runSat = false; runPow = true; runBat = false; runComp = false;
            }
            else if (arg.Equals("--only-battery", StringComparison.OrdinalIgnoreCase))
            {
                runSat = false; runPow = false; runBat = true; runComp = false;
            }
            else if (arg.Equals("--only-computer", StringComparison.OrdinalIgnoreCase))
            {
                runSat = false; runPow = false; runBat = false; runComp = true;
            }
            else if (arg.Equals("--without-battery", StringComparison.OrdinalIgnoreCase))
            {
                runBat = false;
            }
        }

        return new EmulatorOptions
        {
            ServerUrl = server,
            ConfigPath = config,
            PairingCodeSatellite = pairSatellite,
            PairingCodePowered = pairPowered,
            PairingCodeBattery = pairBattery,
            ComputerUserToken = userToken,
            RunSatellite = runSat,
            RunPowered = runPow,
            RunBattery = runBat,
            RunComputer = runComp
        };
    }
}
