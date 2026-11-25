# HomeApps Project Map

- **HEAD**: `3f291a3` (master), last updated 2026-11-25
- **Stack**: .NET 10 (`HomeApps.slnx`), Vue 3 + TypeScript (`src/Web`), PostgreSQL, RabbitMQ (MassTransit), SignalR, MCP (`ModelContextProtocol.AspNetCore`).
- **Tools**: Mac Device Emulator (`tools/mac-device-emulator/MacDeviceEmulator.slnx`).
- **Agent harness**: roles and skills live in `~/.agent-hub`; repo keeps only project-specific overrides (see Agent Overrides).

## Build & Test
- Build all (.NET, warnings = errors): `dotnet build HomeApps.slnx`
- Unit tests (no Docker): `dotnet test tests/<Svc>/<Svc>.Domain.Tests --blame-hang-timeout 30s` (also `*.Application.Tests`, `HomeApps.BuildingBlocks.Tests`, `HomeApps.Plugins.Tests`, `ExternalConfirmationUnitTests` in `Jarvis.Api.Tests`)
- Integration/API tests (requires Docker): `dotnet test HomeApps.slnx --blame-hang-timeout 120s`
- Mac Emulator: `dotnet build tools/mac-device-emulator/MacDeviceEmulator.slnx` & `dotnet test tools/mac-device-emulator/tests/MacDeviceEmulator.Tests/MacDeviceEmulator.Tests.csproj`
- Web: `cd src/Web && npm run type-check && npx eslint . && npm run test:run`
- OpenAPI types: `cd src/Web && npm run api:types`

## Services (`src/`)
Each service has 4 layers: `<Svc>.Domain`, `<Svc>.Application`, `<Svc>.Infrastructure`, `<Svc>.Api`.

- **`src/Auth/`** (`/auth`): Users, OAuth/OIDC, connected apps, JWTs, refresh tokens.
  - Endpoints: `src/Auth/Auth.Api/Endpoints/`
  - Security/JWT: `src/Auth/Auth.Infrastructure/Security/TokenIssuer.cs` (always emits `client_id` on first-party tokens; distinguish first-party vs connected-app by `aud == JwtOptions.Audience`), `ClientRegistry.cs`, `SigningKeys.cs`, `PasswordHasher.cs`
- **`src/Accounts/`** (`/accounts`): Profiles, preferences, user lifecycle.
  - Endpoints: `src/Accounts/Accounts.Api/Endpoints/`
- **`src/Households/`** (`/households`): Households, memberships, roles (`Guest < Member < Admin < Owner`), invites.
  - Endpoints: `src/Households/Households.Api/Endpoints/`
- **`src/Notifications/`** (`/notifications`): In-app & push notifications, SignalR delivery.
  - Endpoints: `src/Notifications/Notifications.Api/Endpoints/`
- **`src/HomeMix/`** (`/homemix`): Plugin/mix items, shared lists, chat/todo plugins (plugin code itself lives in sibling repos, see Plugins).
  - Endpoints: `src/HomeMix/HomeMix.Api/Endpoints/`
- **`src/Devices/`** (`/devices`): Household satellites, pairing codes, device states, built-in computer capabilities.
  - Endpoints: `src/Devices/Devices.Api/Endpoints/` (`CapabilitiesEndpoints.cs`, `DeviceStateEndpoints.cs` [incl. SSE stream `/devices/households/{householdId}/stream`], `HouseholdDevicesEndpoints.cs`, `InternalEndpoints.cs`, `MyDevicesEndpoints.cs`)
  - Domain & State: `src/Devices/Devices.Domain/Devices/` (`Device.cs`, `DeviceState.cs` [status.lowBattery, hysteresis, ApplyValues / changed], `StatusOutcome.cs`, `StatusReport.cs`), `Descriptors/` (`DeviceDescriptor.cs`, `DescriptorParser.cs`)
  - Application & Stream: `src/Devices/Devices.Application/Common/DeviceViews.cs` (`DeviceDescriptorView` at line 145), `Abstractions/IDeviceStream.cs`, `Abstractions/DeviceStreamMessage.cs`, `Frames/ProcessFrames/ProcessFramesHandler.cs`, `Heartbeat/CheckHeartbeats/CheckHeartbeatsHandler.cs`
  - Realtime SSE: `src/Devices/Devices.Api/Stream/` (`DeviceStream.cs`, `StreamSubscription.cs`)
- **`src/Jarvis/`** (`/jarvis`): AI assistant runs, conversations, external MCP gateway, tool confirmations, voice bridge.
  - Endpoints: `src/Jarvis/Jarvis.Api/Endpoints/` (`AssistantEndpoints.cs`, `ConversationEndpoints.cs`)
  - MCP Gateway: `src/Jarvis/Jarvis.Api/Gateway/McpGateway.cs`
  - Voice WebSocket & Session: `src/Jarvis/Jarvis.Api/Voice/` (`VoiceEndpoint.cs` at `/jarvis/voice`, `VoiceSession.cs`)
- **`src/Contracts/`**: `HomeApps.Contracts` (`IIntegrationEvent` records, e.g. `DeviceStateChanged`).
- **`src/BuildingBlocks/`**: `HomeApps.BuildingBlocks.{Domain,Application,Infrastructure,Api,Mcp}`, `HomeApps.Plugins`, `HomeApps.Plugins.Hosting`.
- **`src/Web/`**: Vue 3 SPA (`src/features/{account,admin,auth,devices,home,homemix,households,jarvis,notifications,plugins,wall,widgets}`, `src/api/types/`, `src/api/errorMessage.ts`, `src/shared/i18n/pl.json`).
  - Devices UI: `src/Web/src/features/devices/` (`api.ts`, `stream.ts`, `useDeviceStream.ts`, `assistantFlags.ts`, `pages/HouseholdDevicesTab.vue`, `pages/MyDevicesPage.vue`, `components/DeviceCard.vue`, `components/PairingCodeDialog.vue`, `components/AssistantCard.vue`, `components/ControlWidget.vue`, `components/SensorList.vue`, `components/DeviceEvents.vue`, `components/DeviceAdminPanel.vue`)
  - HomeMix host & widgets: `src/Web/src/features/homemix/` (`useMixHost.ts`, `WidgetSettingsDialog.vue`, `realtime.ts`, `deviceId.ts`, `frameEvents.ts`, `widgets.ts`)

## Plugins (sibling repos)
- Plugins and HomeMix addons are NOT in this repo. Todo lives in `../HomeApps.Todo` (`src/Todo.{Api,Application,Contracts,Domain,Infrastructure,Web}`, `docs/plan.md`, `README.md`).
- Chat and Notes: `../HomeApps.Chat`, `../HomeApps.Notes` (not checked here).
- Plugin contract/hosting in this repo: `src/BuildingBlocks/HomeApps.Plugins`, `src/BuildingBlocks/HomeApps.Plugins.Hosting`, tests `tests/BuildingBlocks/HomeApps.BuildingBlocks.Plugins.Tests` (or `HomeApps.Plugins.Tests`; verify before use).
- Docs: `docs/plugins.md`, `docs/plugin-todo.md`, `docs/plugin-chat.md`, `docs/homemix-authoring.md`, ADR `docs/adr/0009-homemix-addons-in-a-sandbox.md`.

## Tools & Emulators (`tools/`)
- **`tools/mac-device-emulator/`**: macOS satellite & computer emulator (`MacDeviceEmulator.slnx`).
  - Core app: `tools/mac-device-emulator/src/MacDeviceEmulator/`
    - Devices: `Devices/` (`ComputerEmulatorDevice.cs`, `SatelliteEmulatorDevice.cs`, `BatteryEmulatorDevice.cs`, `PoweredEmulatorDevice.cs`)
    - Voice & Utterance: `Services/ContinuousListeningService.cs`, `Services/LocalUtteranceGate.cs`, `Services/VoiceConsentDetector.cs`, `Services/SpeechEchoSuppressor.cs`, `Services/PendingConfirmationStore.cs`
    - Networking/WS: `Services/VoiceWebSocket.cs`, `Services/DeviceWebSocket.cs`, `Services/HomeAppsClient.cs`
    - Abstractions: `Abstractions/` (`IUtteranceGate.cs`, `ISpeechInput.cs`, `ISessionLockMonitor.cs`, `IVoiceWebSocket.cs`, `IListeningIndicator.cs`)
    - Protocol & Config: `Protocol/` (`Descriptors.cs`, `ProtocolMessages.cs`, `VoiceMessage.cs`), `Configuration/` (`EmulatorOptions.cs`, `EmulatorStorage.cs`)
  - Tests: `tools/mac-device-emulator/tests/MacDeviceEmulator.Tests/`

## Tests (`tests/`)
- Per-service: `tests/{Accounts,Auth,Devices,HomeMix,Households,Jarvis,Notifications}/*.Tests` (incl. `tests/Devices/Devices.Application.Tests/Devices/JarvisTabBackendTests.cs`)
- Shared: `tests/BuildingBlocks/HomeApps.BuildingBlocks.{Tests,Plugins.Tests}`, `tests/Integration/`
- JWT test helper: `TestJwt.CreateToken` / `TestJwt.CreateTokenWithClient`

## Specs, Docs & Agent Overrides
- **`specs/`** (owner + `/spec-debate` write; agents read only): `household-jarvis-devices.md` + `-adr.md`, `household-planner.md` + `-adr.md`, `release-3.0.md`, `_TEMPLATE.md`, `CLAUDE_PROJECT_INSTRUCTIONS.md`
- **Stage progress**: `.agents/STATUS.md` (Coordinator only)
- **`docs/`**: `how-a-service-is-built.md`, `adr/0001`–`0009`, `auth.md`, `accounts.md`, `households.md`, `notifications.md`, `homemix.md`, `homemix-authoring.md`, `devices.md`, `devices-protocol.md`, `device-capabilities.md`, `jarvis.md`, `mcp.md`, `plugins.md`, `plugin-chat.md`, `plugin-todo.md`, `web.md`, `deployment.md`, `development.md`, `working-with-the-repo.md`, `schemas/`
- **Agents & skills**: none project-local (`.claude/agents/`, `.claude/skills/` removed). Roles and skills come from `~/.agent-hub`; HomeApps conventions are hub skills `homeapps-*`, profiles `dotnet`, `vue` (see `.claude/CLAUDE.md` `## Agents`).
- **Docker/env**: `compose.yaml`, `Dockerfile`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `nuget.config` (root); `.claude/settings.json`, `.claude/CLAUDE.md`
