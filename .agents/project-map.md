# HomeApps Project Map

- **HEAD**: `d5e7117`
- **Stack**: .NET 10 (`HomeApps.slnx`), Vue 3 + TypeScript (`src/Web`), PostgreSQL, RabbitMQ (MassTransit), SignalR, MCP (`ModelContextProtocol.AspNetCore`).

## Build & Test
- Build all (.NET, warnings = errors): `dotnet build HomeApps.slnx`
- Unit tests (no Docker): `dotnet test tests/<Svc>/<Svc>.Domain.Tests --blame-hang-timeout 30s` (also `*.Application.Tests`, `HomeApps.BuildingBlocks.Tests`, `HomeApps.Plugins.Tests`, `ExternalConfirmationUnitTests` in `Jarvis.Api.Tests`)
- Integration/API tests (requires Docker): `dotnet test HomeApps.slnx --blame-hang-timeout 120s`
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
- **`src/HomeMix/`** (`/homemix`): Plugin/mix items, shared lists, chat/todo plugins.
  - Endpoints: `src/HomeMix/HomeMix.Api/Endpoints/`
- **`src/Devices/`** (`/devices`): Household satellites, pairing codes, device states, built-in computer capabilities.
  - Endpoints: `src/Devices/Devices.Api/Endpoints/` (`CapabilitiesEndpoints.cs`, `DeviceStateEndpoints.cs`, `HouseholdDevicesEndpoints.cs`, `InternalEndpoints.cs`, `MyDevicesEndpoints.cs`)
- **`src/Jarvis/`** (`/jarvis`): AI assistant runs, conversations, external MCP gateway, tool confirmations, voice bridge.
  - Endpoints: `src/Jarvis/Jarvis.Api/Endpoints/` (`AssistantEndpoints.cs`, `ConversationEndpoints.cs`)
  - MCP Gateway: `src/Jarvis/Jarvis.Api/Gateway/McpGateway.cs`
  - Voice: `src/Jarvis/Jarvis.Api/Voice/`
- **`src/Contracts/`**: `HomeApps.Contracts` (`IIntegrationEvent` records).
- **`src/BuildingBlocks/`**: `HomeApps.BuildingBlocks.{Domain,Application,Infrastructure,Api,Mcp}`, `HomeApps.Plugins`, `HomeApps.Plugins.Hosting`.
- **`src/Web/`**: Vue 3 SPA (`src/features/{account,admin,auth,devices,home,homemix,households,jarvis,notifications,plugins,wall,widgets}`, `src/api/types/`).

## Tests (`tests/`)
- Per-service: `tests/{Accounts,Auth,Devices,HomeMix,Households,Jarvis,Notifications}/*.Tests`
- Shared: `tests/BuildingBlocks/HomeApps.BuildingBlocks.{Tests,Plugins.Tests}`, `tests/Integration/`
- JWT test helper: `TestJwt.CreateToken` / `TestJwt.CreateTokenWithClient`

## Specs, Docs & Agent Overrides
- **`specs/`**: `household-jarvis-devices.md`, `household-jarvis-devices-adr.md`, `_TEMPLATE.md`, `CLAUDE_PROJECT_INSTRUCTIONS.md`
- **`docs/`**: `how-a-service-is-built.md`, `adr/0001`–`0009`, `auth.md`, `accounts.md`, `households.md`, `notifications.md`, `homemix.md`, `homemix-authoring.md`, `devices.md`, `devices-protocol.md`, `device-capabilities.md`, `jarvis.md`, `mcp.md`, `plugins.md`, `plugin-chat.md`, `plugin-todo.md`, `web.md`, `deployment.md`, `development.md`, `working-with-the-repo.md`
- **`.claude/agents/`**: `developer.md`, `tester.md`, `reviewer.md`, `devils-advocate.md`, `docs-writer.md`, `dotnet-architect.md`, `vue-architect.md`
- **`.claude/skills/`**: `run-feature`, `spec-debate`, `dotnet-conventions`, `vue-conventions`, `testing-standards`, `code-review-checklist`
