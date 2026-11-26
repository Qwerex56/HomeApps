# HomeApps Project Map

- **HEAD**: `43af4d4` (branch `master`), last updated 2026-11-26 (refreshed from `67293e9`; release 3.0 stage 4 merged via PR #76; `.github/workflows/release.yaml` added)
- **Stack**: .NET 10 (`HomeApps.slnx`), Vue 3 + TypeScript (`src/Web`), PostgreSQL, RabbitMQ (MassTransit), SignalR, MCP (`ModelContextProtocol.AspNetCore`). npm scope `@home-apps/*`.
- **API versioning**: every service serves `/v1/...` (`MapVersionedGroup`, `EndpointExtensions.MapVersionedEndpoints`); legacy unversioned routes answer with `Deprecation`/`Sunset` (2028-01-01). Unversioned by design: OAuth and sockets. Clients and tests use `/v1`.
- **Tools**: Mac Device Emulator (`tools/mac-device-emulator/MacDeviceEmulator.slnx`).
- **Agent harness**: roles and skills live in `~/.agent-hub`; repo keeps only project-specific overrides (see Agent Overrides).

## Build & Test
- Build all (.NET, warnings = errors): `dotnet build HomeApps.slnx`
- Unit tests (no Docker): `dotnet test tests/<Svc>/<Svc>.Domain.Tests --blame-hang-timeout 30s` (also `*.Application.Tests`, `HomeApps.BuildingBlocks.Tests`, `HomeApps.Plugins.Tests`, `ExternalConfirmationUnitTests` in `Jarvis.Api.Tests`)
- Integration/API tests (requires Docker): `dotnet test HomeApps.slnx --blame-hang-timeout 120s`
- Mac Emulator: `dotnet build tools/mac-device-emulator/MacDeviceEmulator.slnx` & `dotnet test tools/mac-device-emulator/tests/MacDeviceEmulator.Tests/MacDeviceEmulator.Tests.csproj`
- Web: `cd src/Web && npm run type-check && npx eslint . && npm run test:run`
- Versioning tests: `tests/<Svc>/<Svc>.Api.Tests/VersioningTests.cs` (dual route, legacy headers, unversioned sockets)
- OpenAPI types: `cd src/Web && npm run api:types`

## Services (`src/`)
Each service has 4 layers: `<Svc>.Domain`, `<Svc>.Application`, `<Svc>.Infrastructure`, `<Svc>.Api`.

- **`src/Auth/`** (`/auth`): Users, OAuth/OIDC, connected apps, JWTs, refresh tokens.
  - Endpoints: `src/Auth/Auth.Api/Endpoints/`, `src/Auth/Auth.Api/ConnectedApps/ConnectedAppEndpoints.cs`
  - Security/JWT: `src/Auth/Auth.Infrastructure/Security/TokenIssuer.cs` (always emits `client_id` on first-party tokens; distinguish first-party vs connected-app by `aud == JwtOptions.Audience`), `ClientRegistry.cs`, `SigningKeys.cs`, `PasswordHasher.cs`
- **`src/Accounts/`** (`/accounts`): Profiles, preferences, avatars, user lifecycle.
  - Endpoints: `src/Accounts/Accounts.Api/Endpoints/` (`MeEndpoints.cs` [`POST /users/v1/me/avatar`], `AdminEndpoints.cs`, `ReactivationEndpoints.cs`)
  - Use cases: `Accounts.Application/{MyAccount,Admin}/<Feature>/` (one folder per command/query; `MyAccount/UploadMyAvatar`, `GetUserAvatar`); `Common/AvatarImage.cs`, `Abstractions/IAvatarStore.cs`
  - Storage: `Accounts.Infrastructure/Persistence/FileAvatarStore.cs` (volume `homeapps_avatars` at `/var/data/avatars`); startup `Jobs/ProfileRepublishOnStart.cs` + `StartupMarker.cs` (migration `StartupMarkers`)
  - Domain: `Accounts.Domain/Users/` (`AvatarUrl.cs`, `Profile.cs`, `User.Profile.cs`, `Events/UserProfileUpdated.cs`)
  - Server about (admin): `GET /users/v1/admin/users/server/about` in `AdminEndpoints.cs`; `Accounts.Application/Admin/GetServerAbout/` (`GetServerAboutHandler`, `ServerAboutView`, `ServiceAboutView`, `PluginAboutView`), port `Abstractions/IServerInfo.cs`, adapter `Accounts.Infrastructure/About/ServerInfo.cs`; wiring in `Accounts.Infrastructure/DependencyInjection.cs`
  - Shared about/migrations plumbing: `BuildingBlocks.Api/ServiceVersion.cs`, `BuildingBlocks.Application/Abstractions/IAppliedMigrations.cs`, `BuildingBlocks.Infrastructure/Persistence/AppliedMigrations.cs`
- **`src/Households/`** (`/households`): Households, memberships, roles (`Guest < Member < Admin < Owner`), invites.
  - Endpoints: `src/Households/Households.Api/Endpoints/` (`HouseholdEndpoints.cs` [incl. `PUT /households/v1/{id}/timezone`], `MemberEndpoints.cs`, `InviteEndpoints.cs`, `AssistantEndpoints.cs`, `InternalEndpoints.cs`, `AdminEndpoints.cs`)
  - Application is split per feature: `Households.Application/{Households,Members,Invites}/<Feature>/`; time zone rule (household, else owner, else `Europe/Warsaw`): `Households.Application/Common/HouseholdTimeZones.cs`; account sync: `AccountsSync/AccountsSync.cs`
- **`src/Notifications/`** (`/notifications`): In-app & push notifications. No SignalR hub here: push goes out via port `Notifications.Application/Abstractions/IPushSender.cs` (`SendAsync` → `PushOutcome`); recipients/targets in `Notifications.Domain/Recipients/` (`Recipient.cs`, `PushTarget.cs`).
  - Endpoints: `src/Notifications/Notifications.Api/Endpoints/` (`InAppEndpoints.cs` → `/notifications/v1/in-app`, list / mark read / mark all read)
  - In-app center: `Notifications.Application/InApp/` (`ListInApp`, `MarkInAppRead`, `MarkAllInAppRead`), `Notifications.Domain/InApp/InAppNotification.cs`, `Notifications.Infrastructure/Persistence/InAppNotificationRepository.cs` (table `InAppNotifications`)
  - Realtime to the browser: the SignalR hubs are in HomeMix (`HomeMixHub`) and Chat (`HubRealtime`), not here.
- **`src/HomeMix/`** (`/homemix`): Plugin/mix items, shared lists, chat/todo plugins (plugin code itself lives in sibling repos, see Plugins).
  - Endpoints: `src/HomeMix/HomeMix.Api/Endpoints/` (`BoardEndpoints`, `PackageEndpoints`, `StoreEndpoints`, `TriggerEndpoints`, `InstallationEndpoints`, `DataEndpoints`, `DeviceEndpoints`, `NotifyEndpoints`, `AdminEndpoints`)
  - Addon data store: `HomeMix.Api/Endpoints/DataEndpoints.cs` (`/homemix/api/installations/{id}/data/{collection}`, GET/PUT/DELETE by key, `scope`, `version`; `AllowAssistant`); use cases `HomeMix.Application/Data/` (`PutDocument`, `GetDocument`, `ListDocuments`, `PurgeRemovedData`); domain `HomeMix.Domain/Data/AddonDocument.cs` (`ExpiresAt`, `IsExpired`); repo `HomeMix.Infrastructure/Persistence/DocumentRepository.cs`; migration `DocumentExpiration`
  - Boards (widget layout save/load): `HomeMix.Api/Endpoints/BoardEndpoints.cs` → `GET|PUT /homemix/api/households/{householdId}/boards/{place}` (`MapVersionedGroup`); `HomeMix.Application/Boards/{SaveBoard/SaveBoardCommand.cs, BoardItemInput.cs, GetBoard/}`; domain `HomeMix.Domain/Boards/WidgetBoard.cs` (widget widths 1-8). Board save has NO layout version / concurrency token (last write wins; `SaveBoardCommand(HouseholdId, Place, Items)`).
  - Realtime hub: `HomeMix.Api/Realtime/HomeMixHub.cs` (`Hub`, joins user and household groups; `RealtimeExtensions.cs` maps `HomeMixHub.Path`), sender `HubRealtimeSender.cs`, port `HomeMix.Application/Realtime/IRealtimeSender.cs` + `RealtimeEvents.cs`; web client `src/Web/src/features/homemix/realtime.ts` (`@microsoft/signalr`)
  - Startup: fails before migrations if `HOMEMIX_CLIENT_SECRET` is empty (`HomeMix.Api/Program.cs`)
  - Devices client: `src/HomeMix/HomeMix.Infrastructure/Devices/DevicesServiceClient.cs`, `DevicesHttp.cs`
- **`src/Devices/`** (`/devices`): Household satellites, pairing codes, device states, built-in computer capabilities.
  - Endpoints: `src/Devices/Devices.Api/Endpoints/` (`CapabilitiesEndpoints.cs`, `DeviceStateEndpoints.cs` [incl. SSE stream `/v1/devices/households/{householdId}/stream`], `HouseholdDevicesEndpoints.cs`, `InternalEndpoints.cs`, `MyDevicesEndpoints.cs`)
  - Startup: fails before migrations if `DEVICES_CLIENT_SECRET` is empty (`Devices.Api/Program.cs`, `DevicesOptions.cs`); MCP tools: `Devices.Api/Mcp/DeviceToolsServer.cs`
  - Domain & State: `src/Devices/Devices.Domain/Devices/` (`Device.cs`, `DeviceState.cs` [status.lowBattery, hysteresis, ApplyValues / changed], `StatusOutcome.cs`, `StatusReport.cs`), `Descriptors/` (`DeviceDescriptor.cs`, `DescriptorParser.cs`)
  - Application & Stream: `src/Devices/Devices.Application/Common/DeviceViews.cs` (`DeviceDescriptorView` at line 145), `Abstractions/IDeviceStream.cs`, `Abstractions/DeviceStreamMessage.cs`, `Frames/ProcessFrames/ProcessFramesHandler.cs`, `Heartbeat/CheckHeartbeats/CheckHeartbeatsHandler.cs`
  - Realtime SSE: `src/Devices/Devices.Api/Stream/` (`DeviceStream.cs`, `StreamSubscription.cs`)
- **`src/Jarvis/`** (`/jarvis`): AI assistant runs, conversations, external MCP gateway, tool confirmations, voice bridge.
  - Endpoints: `src/Jarvis/Jarvis.Api/Endpoints/` (`AssistantEndpoints.cs`, `ConversationEndpoints.cs` [rename: `Requests/RenameConversationRequest.cs`; list search `?q=`]; use cases `Jarvis.Application/Conversations/`); household ceilings & devices: `Jarvis.Infrastructure/Gateway/HouseholdsCeilings.cs`, `HouseholdDevices.cs`, `AuthDelegation.cs`
  - MCP Gateway: `src/Jarvis/Jarvis.Api/Gateway/McpGateway.cs`
  - Tool confirmations (pending call / approval): domain `Jarvis.Domain/ToolCalls/ToolCall.cs` (`Status` NeedsConfirmation → Done/Failed/Denied/Expired; `ConfirmBy` = +10 min `ConfirmationWindow`; `StatusAt(now)`), `ToolCallStatus.cs`, `ToolCallErrors.cs`; repo `Jarvis.Infrastructure/Persistence/ToolCallRepository.cs`; use cases `Jarvis.Application/ToolCalls/{AwaitConfirmation,AwaitExternalConfirmation,RecordCall}/` (`AwaitConfirmationHandler.cs` publishes to `IRunFeed`); execution `Jarvis.Infrastructure/Tools/GatewayToolExecutor.cs`; approval via bus `Jarvis.Infrastructure/Messaging/AssistantConfirmedConsumer.cs` (`AssistantConfirmed`); cards `Jarvis.Infrastructure/Messaging/Cards.cs`, `Jarvis.Application/Common/ActionView.cs`; live feed `Jarvis.Infrastructure/Live/RunFeed.cs` (`IRunFeed`).
  - Confirmation HTTP: `Jarvis.Api/Endpoints/AssistantEndpoints.cs` — `GET /confirmations?pending=` (line 39, group prefix not verified, handler `Jarvis.Application/Actions/ListPendingConfirmations/`), `POST /confirmations/{id}` decide (line 55, approve/deny). Person's own token only; connected apps refused (line 22-28).
  - Web: `src/Web/src/features/jarvis/` (`api.ts`, `ActionCard.vue`, `__tests__/ActionCard.spec.ts`). Chat plugin (`../HomeApps.Chat`) consumes the same endpoints: `Chat.Api/Endpoints/AssistantEndpoints.cs`, `Chat.Application/Assistant/ReceiveAnswer/`, `Chat.Web/src/api.ts` (`ActionStatus`, `decide(messageId, confirmationId, approve)`), `Chat.Web/src/timeline.ts`; realtime `Chat.Api/Realtime/HubRealtime.cs` (port `Chat.Application/Abstractions/IRealtime.cs`, `SendAsync(ChatSignal)`, `EvictAsync`).
  - Voice WebSocket & Session: `src/Jarvis/Jarvis.Api/Voice/` (`VoiceEndpoint.cs` at `/jarvis/voice`, `VoiceSession.cs`)
- **`src/Contracts/`**: `HomeApps.Contracts` (`IIntegrationEvent` records, e.g. `DeviceStateChanged`).
- **`src/BuildingBlocks/`**: `HomeApps.BuildingBlocks.{Domain,Application,Infrastructure,Api,Mcp}`, `HomeApps.Plugins`, `HomeApps.Plugins.Hosting`.
- **`src/Web/`**: Vue 3 SPA, API calls on `/v1` paths (`src/api/http.ts`), plugin UI SDK `@home-apps/plugin-sdk` (`src/features/{account,admin,auth,devices,home,homemix,households,jarvis,notifications,plugins,wall,widgets}`, `src/api/types/`, `src/api/errorMessage.ts`, `src/shared/i18n/pl.json`).
  - Devices UI: `src/Web/src/features/devices/` (`api.ts`, `stream.ts`, `useDeviceStream.ts`, `assistantFlags.ts`, `pages/HouseholdDevicesTab.vue`, `pages/MyDevicesPage.vue`, `components/DeviceCard.vue`, `components/PairingCodeDialog.vue`, `components/AssistantCard.vue`, `components/ControlWidget.vue`, `components/SensorList.vue`, `components/DeviceEvents.vue`, `components/DeviceAdminPanel.vue`)
  - HomeMix host & widgets: `src/Web/src/features/homemix/` (`useMixHost.ts`, `api.ts`, `WidgetSettingsDialog.vue`, `realtime.ts`, `deviceId.ts`, `frameEvents.ts`, `widgets.ts`)
  - Widget board editor/grid (resize, widths, snapping): `src/Web/src/features/widgets/` — `board.ts` (assembles board, `widthsFor`/`fit` against saved layout), `layout.ts` (8 columns; `columnsFor` 3/5/8 by width <560/<840; `widthOf` Small 2/Medium 4/Large 6; `arrange`), `useGridWidth.ts` (ResizeObserver → columns and row height), `BoardEditor.vue` (`@resized="snap"`, save), `WidgetGrid.vue`, `WidgetTile.vue`, `HouseholdCard.vue`, `__tests__/`. Board save/load calls: `features/homemix/api.ts` and HomeMix `BoardEndpoints` (see HomeMix section).
  - Plugin registry `features/plugins/registry.ts`
  - Admin server about: `src/Web/src/features/admin/pages/ServerAboutPage.vue`, `features/admin/api.ts`; router `src/Web/src/app/router.ts`
  - HomeMix addon SDK & local CLI (JS, not in `npm` workspace `src/Web/src`): `src/Web/homemix/sdk/src/{client,protocol}.ts`, `src/Web/homemix/cli/host/host.js`, `src/Web/homemix/create-homemix/`, `src/Web/homemix/vue/`

## Plugins (sibling repos)
- Plugins and HomeMix addons are NOT in this repo. Todo lives in `../HomeApps.Todo` (`src/Todo.{Api,Application,Contracts,Domain,Infrastructure,Web}`, `docs/plan.md`, `README.md`).
- Chat and Notes: `../HomeApps.Chat`, `../HomeApps.Notes` (not checked here).
- Plugin contract/hosting in this repo: `src/BuildingBlocks/HomeApps.Plugins`, `src/BuildingBlocks/HomeApps.Plugins.Hosting` (`PluginEndpoints.cs`, `DevicesHttpClient.cs`, `HouseholdsHttpDirectory.cs`; calls on `/v1`), tests `tests/BuildingBlocks/HomeApps.BuildingBlocks.Plugins.Tests` or `tests/BuildingBlocks/HomeApps.Plugins.Tests` (verify before use).
- Shared API kit: `src/BuildingBlocks/HomeApps.BuildingBlocks.Api/Endpoints/` (`EndpointExtensions.cs`, `VersionedRouteBuilder.cs`), `HomeAppsApiExtensions.cs`.
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
- **`specs/`** (owner + `/spec-debate` write; agents read only): `household-jarvis-devices.md` + `-adr.md`, `household-planner.md` + `-adr.md`, `release-3.0.md` + `release-3.0-adr.md` (3.0 stages 0-4, all merged), `fix-3.0-post-release.md` (post-release fix spec, no ADR yet), `_TEMPLATE.md`, `CLAUDE_PROJECT_INSTRUCTIONS.md`
- **Stage progress**: `.agents/STATUS.md` (committed, Coordinator only). Release 3.0 runs `/run-feature @specs/release-3.0.md --base master "Stage N"`, branches `feat/release-3.0-stage-N`. Stage 4 row: PR #76 merged.
- **Changelog & upgrade notes**: `CHANGELOG.md` (`### Upgrading` section per release; 3.0 stage 1 added the avatars volume and migrations; `docs/homemix.md`, `docs/accounts.md`, `docs/development.md` touched in stage 3).
- **Release CI**: `.github/workflows/release.yaml` (added in 3.0 stage 4; see `docs/working-with-the-repo.md#releases`). `HomeApps.slnx` no longer lists `.env` as a solution item.
- **Doc index**: `docs/README.md` (reading path and topic table; start here). Upgrade path 2.4 → 3.0 in `docs/deployment.md#upgrading-from-24-to-30`.
- **`docs/`**: `README.md`, `how-a-service-is-built.md` (API versioning section), `adr/0001`–`0009`, `auth.md`, `accounts.md`, `households.md`, `notifications.md`, `homemix.md`, `homemix-authoring.md`, `devices.md`, `devices-protocol.md`, `device-capabilities.md`, `jarvis.md`, `mcp.md`, `plugins.md`, `plugin-chat.md`, `plugin-todo.md`, `web.md`, `deployment.md`, `development.md`, `working-with-the-repo.md`, `schemas/`
- **Agents & skills**: none project-local (`.claude/agents/`, `.claude/skills/` removed). Roles and skills come from `~/.agent-hub`; HomeApps conventions are hub skills `homeapps-*`, profiles `dotnet`, `vue` (see `.claude/CLAUDE.md` `## Agents`).
- **Docker/env**: `compose.yaml`, `compose.dev.yaml`, `Dockerfile`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `nuget.config`, `compose.prod.yaml` (volume `homeapps_avatars`), `scripts/pack.sh` (root); `src/Accounts/Accounts.Api/Dockerfile`; `.claude/settings.json`, `.claude/CLAUDE.md`
