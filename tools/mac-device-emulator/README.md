# Mac Device Emulator (`tools/mac-device-emulator`)

A standalone .NET 10 solution providing a macOS-based emulator for HomeApps smart home and assistant test devices according to the `homeapps.device/1` protocol (stage **H-5**, spec 2.13, ADR decisions #9 and #10).

It allows testing HomeApps without physical hardware, running 4 distinct devices representing different roles and link modes.

## Emulated Devices

> **Important**: When creating pairing codes in HomeApps (via Web UI or API), always ensure the device name includes `(test)` (e.g. `Mac Satellite (test)`, `Mac Lamp (test)`, `Mac Weather (test)`). The emulator checks this name upon pairing and logs a warning if `test` is missing.

1. **Mac Satellite (`satellite`, `purpose: assistant`)**
   - Paired using one-time pairing code via `POST /devices/pair`.
   - Connects to WebSocket `/devices/link` with device token from `POST /auth/devices/token`.
   - Capabilities: `voice`, `audio`. (Note: Declares `voice` capability in its descriptor for assistant discovery, but does not stream audio to `/jarvis/voice` as a software emulator; it implements the `say` action and volume/mute controls).
   - Controls: Mac volume and mute (via `osascript`).
   - Actions: `say` (speaks text aloud on Mac using `say` CLI; text is capped at 200 characters and executed without blocking WebSocket heartbeats).

2. **Mac Smart Home Powered (`powered`, `purpose: smart_home`)**
   - Paired using one-time pairing code via `POST /devices/pair`.
   - Connects to WebSocket `/devices/link`.
   - Capabilities: `audio`, `switch` (id: `lamp`).
   - Controls: Mac volume & mute, simulated desk lamp (`lamp.on`).
   - Sensors: CPU load (`cpu_load` via `top` / `sysctl`).
   - Periodic state & heartbeat status report every 60 seconds.

3. **Mac Smart Home Battery (`battery`, `purpose: smart_home`, `sleepy: true`)**
   - Paired using one-time pairing code via `POST /devices/pair`.
   - No persistent WebSocket link: wakes and sends batch frames via `POST /devices/report` every 60 seconds.
   - Reports real Mac battery percentage in `status.battery` and `state.battery` via `pmset -g batt`. If no battery is present (e.g. Mac mini / desktop Mac), it omits the `battery` sensor values and sends only status heartbeats.
   - Can be stopped independently (`--without-battery` or by exiting) to test the 2.5-period (150 s) silence/missing detection in HomeApps.

4. **Mac Computer (`computer`, `purpose: computer`)**
   - Connects to `/devices/link` using user's personal Bearer token.
   - Name: `MacBook (test)` with purpose `computer`.
   - Controls: Mac volume & mute.
   - Actions: `say`.
   - User token note: Bearer tokens expire. Obtain a token by logging into HomeApps Web UI (`localStorage.getItem('token')`) or via `POST /auth/login`. When the token expires, update `--user-token` or `HOMEAPPS_USER_TOKEN`.
   - **Continuous listening (H-8)**:
     - Runs locally on the computer (never on ESP satellites) when `RunComputer` is enabled (disable with `--no-listen`).
     - Local "is this for me?" gate evaluates wake words, mute state, macOS session lock (`ioreg`), and self-voice echo suppression before anything leaves the machine.
     - Only accepted utterances are sent to `/jarvis/voice?speech=local` (`hello` -> `say` -> `transcript`/`reply`/`done`).
     - Changes/actions requiring confirmation are confirmed **on screen only** (`/approve`, `/deny` via `POST /jarvis/api/confirmations/{id}`), never by voice.
     - Visible listening indicator (`[MIC: LISTENING]`, `[MIC: MUTED]`, `[MIC: PAUSED - SESSION LOCKED]`, `[MIC: PAUSED - JARVIS SPEAKING]`) and mute toggle (`/mute`, `/unmute`, `/toggle-mute`, or `--muted`).

## Credentials & Persistence

Paired device secrets and metadata are persisted locally in `~/.homeapps/mac-devices.json` (or a custom path via `--config`).
The file is created with owner-only permissions (`0600` on Unix/macOS) and updated atomically with concurrency locking across device runner tasks.
When `devices.device_unpaired` is returned by the server, the stored secret is cleared and the device returns to pairing mode.

## Usage

Build the solution:
```bash
dotnet build tools/mac-device-emulator/MacDeviceEmulator.slnx
```

Run tests:
```bash
dotnet test tools/mac-device-emulator/tests/MacDeviceEmulator.Tests/MacDeviceEmulator.Tests.csproj
```

### Command Line Options

```bash
dotnet run --project tools/mac-device-emulator/src/MacDeviceEmulator/MacDeviceEmulator.csproj -- [options]
```

Options:
- `--server <url>`: HomeApps server URL (default: `https://app.localhost`).
- `--config <path>`: Path to local state JSON file (default: `~/.homeapps/mac-devices.json`).
- `--pair-satellite <code>`: 8-character pairing code for the Satellite device.
- `--pair-powered <code>`: 8-character pairing code for the Powered smart home device.
- `--pair-battery <code>`: 8-character pairing code for the Battery device.
- `--user-token <token>`: User Bearer token to connect the Computer device. Can also be set via the `HOMEAPPS_USER_TOKEN` environment variable to prevent leaking tokens in `ps` or shell history.
- `--only-satellite`: Run only the Satellite device.
- `--only-powered`: Run only the Powered device.
- `--only-battery`: Run only the Battery device.
- `--only-computer`: Run only the Computer device.
- `--without-battery`: Run all devices except Battery (useful to simulate battery device going offline / silent).
- `--no-listen`: Disable H-8 continuous listening when running the Computer device.
- `--muted`: Start continuous listening muted.
- `--stt-command <exe>` / `--stt-arg <arg>`: Optional local speech-to-text command whose stdout lines feed the local gate.
- `--wake-words <w1,w2,...>`: Override default wake words (`jarvis`, `hej jarvis`, `hey jarvis`, etc.).
