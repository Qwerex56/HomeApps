# JARVIS

The assistant of HomeApps (plan `plans/features/jarvis-v1.md`). Built in stages: **J2a** (this) is the MCP gateway;
the agent loop, `POST /jarvis/api/ask` and the "JARVIS" page follow in J2b and J2c.

## The gateway `/jarvis/mcp`

One MCP server (Streamable HTTP) in front of every MCP server of the instance, so an MCP client connects once:

| Source | Where (Docker network) | Tool names |
|---|---|---|
| Households, Accounts | `http://households:8080/mcp`, `http://accounts:8080/mcp` | `households_my_households`, `accounts_my_profile` |
| Plugins in `HOMEAPPS_PLUGINS` | the manifest's `endpoints.mcp` at `Jarvis:PluginUrl` (`http://{id}:8080`) | `todo_add_task`, `chat_send_message` |
| HomeMix | `http://homemix:8080/mcp` | as HomeMix names them (`pomodoro_add_session`) |

- **Per person.** `tools/list` asks every source with the caller's token; nothing is kept between people. A source
  that does not answer leaves out only its own tools. A second tool with a name already taken is left out and logged.
- **Always as an assistant.** A connected app's token (`aud` = `Jarvis:Resource`) or a person's own token is exchanged
  for Auth's delegated token (`POST /internal/delegation`, 5 minutes, kept until shortly before it expires), so every
  source sees `act` and refuses changes to accounts and households (JV13). A delegated token passes as it is.
- **Resources** pass through with the source in front of the URI: `homemix:homemix://pomodoro/summary`.
- Without a token: `401` with `WWW-Authenticate: Bearer resource_metadata=".../.well-known/oauth-protected-resource/jarvis/mcp"`
  (RFC 9728), which names Auth as the authorization server. MCP clients start the OAuth flow from there
  ([auth.md](auth.md#connected-apps)).

Settings (`Jarvis`):

| Key | |
|---|---|
| `Resource` | the gateway's public address, e.g. `https://home-apps.dev/jarvis/mcp`; also in Auth's `Auth:ConnectedApps:Resources` |
| `Sources` | `Name`, `Url`, `Prefix` (empty for HomeMix) |
| `Plugins`, `PluginUrl`, `PluginUrls:{id}` | from `HOMEAPPS_PLUGINS`; `PluginUrls` for a plugin run from the IDE |
| `AuthUrl`, `ClientId`, `ClientSecret` | Auth's `Service` client (`jarvis-service`) |

## Connecting Claude Code (dev)

Run the stack with `--profile services` (or JARVIS from the IDE on `:5700`, routed by `dynamic/dev-routes.yaml`), then:

```bash
claude mcp add --transport http homeapps https://app.localhost/jarvis/mcp
```

In Claude Code, `/mcp` → `homeapps` → sign in: the browser opens HomeApps' consent screen (`/connect`), and the app
shows up under "Połączone aplikacje" in the account settings. claude.ai: Settings → Connectors → Add custom connector,
same address (it needs the gateway reachable from the internet, so production only).
