# JARVIS

The assistant of HomeApps (plan `plans/features/jarvis-v1.md`), since 2.3.0: one MCP gateway for every tool of the
instance, an agent that answers on the "JARVIS" page and as `@Jarvis` in the chat plugin, and the same gateway for
your own Claude Code or claude.ai. [Production](#production) says how to switch it on.

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

## The agent (J2b)

- **Ask:** `POST /jarvis/api/ask` `{text, householdId?}` with the person's own token (an assistant's is refused)
  answers `{runId, text, actions[]}`. A channel sends `AssistantInvoked` instead and gets `AssistantWorking`, then
  `AssistantResponded` with the same `reference`; the run happens in a background worker, not inside the consumer.
- **Brain:** `claude -p` per run (`Jarvis:ClaudeCode`), only the gateway as MCP server with the person's delegated token
  (reference `run:{id}`) and the `X-Jarvis-Run` header, no built-in tools, a turn limit and a timeout. Without
  `OAuthToken`, or with `Jarvis:Enabled` false, JARVIS answers that he is switched off; over `Jarvis:DailyBudgetUsd`
  (whole home, since midnight UTC) that the budget is spent.
- **Actions:** in a run the gateway logs every call. `readOnlyHint` = a read (not on the card); `destructiveHint: false`
  = done and shown; anything else (MCP's default counts as destructive) is not called but waits 10 minutes for
  `POST /jarvis/api/confirmations/{id}` `{approve}` or `AssistantConfirmed`, only from the person who asked.
- **Data:** runs and calls in `homeapps_jarvis`; content erased after 7 days, rows after 30; everything on `UserErased`.

## Conversations in the app (J2c)

- Page **JARVIS** (`/jarvis/:conversationId?`) in the menu: the person's conversations, the open one, the action card with
  Confirm / Cancel. The household picked in the header goes along as JARVIS's default.
- API, only the owner's own (someone else's answers 404, the server's Owner too):
  `GET/POST /jarvis/api/conversations`, `DELETE /jarvis/api/conversations/{id}`,
  `GET /jarvis/api/conversations/{id}/messages`, `POST /jarvis/api/conversations/{id}/messages` `{text, householdId?}`.
- Sending answers with server-sent events: `text` (JSON string pieces), `action` (`ActionView`, as the gateway logs it),
  then `message` (the kept answer with its card) or `error` (`{code, detail}`). JARVIS sees the last 20 messages.
- Messages stay until the person deletes the conversation (or the account is erased); the run behind an answer follows
  the log's retention, so after 30 days the message keeps its text but loses its card.
- Traefik sends only `/jarvis/api`, `/jarvis/mcp`, `/jarvis/voice` and the metadata to the service; `/jarvis` itself is the web app.

## Voice (V0a)

JARVIS hears and speaks through `/jarvis/voice`, a WebSocket that every listener uses: the Mac bridge
(`src/Jarvis/Jarvis.VoiceBridge`, below) now, satellites later. The listener only carries sound; speech to text, the
agent and text to speech all happen on the server. Plan: `plans/features/jarvis-voice-bridge.md`.

- **Token:** the person's, as `Authorization: Bearer`: an app token from "Połączone aplikacje" (resource
  `https://<domain>/jarvis/mcp`, the same OAuth as Claude Code) or a normal one.
- **Listener → server:** text frames `{"type":"hello","conversationId"?,"householdId"?}`, `{"type":"start"}`,
  `{"type":"stop"}`, `{"type":"cancel"}`; between `start` and `stop` binary frames of 16 kHz 16-bit mono PCM (at most
  `Jarvis:Voice:MaxSeconds`, 30 s).
- **Server → listener:** `ready`, then per turn `transcript` `{text}`, `reply` `{conversationId, text, actions,
  awaiting}`, for each sentence `audio` `{format: "wav", text}` followed by one binary frame with the WAV, then `done`
  (`cancelled: true` after `cancel` or a new `start`); or `error` `{code}` (`voice.engine_unavailable`, ...).
- **Conversation:** spoken turns go to the person's conversation on the JARVIS page (channel `voice`, a shorter answer
  for the ear); the first turn without `conversationId` starts one. Audio is never kept, the text like any message.
- **Confirmations by voice:** when `awaiting` is true, the listener records the next answer without the wake word. A
  short "tak / potwierdzam / zrób to" confirms the waiting actions, "nie / anuluj" cancels them (without the model);
  anything longer is a new request. The buttons on the JARVIS page keep working.
- **Engines:** `Jarvis:Voice:SttUrl` and `TtsUrl` speak the OpenAI audio API (`/v1/audio/transcriptions`,
  `/v1/audio/speech`), so Speaches, a whisper server or a cloud service is only a URL (`ApiKey` for a cloud one).
  Defaults: `SttModel` `deepdml/faster-whisper-large-v3-turbo-ct2`, `TtsModel` `speaches-ai/piper-pl_PL-gosia-medium`,
  `TtsVoice` `gosia`, `Language` `pl`. `Jarvis:Voice:Enabled` false (the default) answers 503.

### Speaches on the Mac

The home server (Dell Wyse 5070) is too slow for Whisper, so for tests the engines run on the Mac:

```bash
docker compose -f compose.voice.yaml up -d
# once: the models into the volume
curl -X POST http://localhost:8000/v1/models/deepdml/faster-whisper-large-v3-turbo-ct2
curl -X POST http://localhost:8000/v1/models/speaches-ai/piper-pl_PL-gosia-medium
# check: Polish speech out, then back in
curl -s localhost:8000/v1/audio/speech -H 'Content-Type: application/json' \
  -d '{"model":"speaches-ai/piper-pl_PL-gosia-medium","voice":"gosia","input":"Cześć, tu JARVIS.","response_format":"wav"}' -o hej.wav
curl -s localhost:8000/v1/audio/transcriptions -F file=@hej.wav \
  -F model=deepdml/faster-whisper-large-v3-turbo-ct2 -F language=pl
```

Other voices and models: `curl 'localhost:8000/v1/registry?task=text-to-speech'` (look for `piper-pl_PL`) and
`?task=automatic-speech-recognition` (look for `turbo`); a different id goes to `Jarvis__Voice__SttModel` / `TtsModel`. Then on the server
`JARVIS_VOICE_URL=http://<Mac's address>:8000` in `.env` (give the Mac a fixed address in the router); in dev the
compose file already points at `host.docker.internal:8000`.

### The Mac bridge (V0b)

`src/Jarvis/Jarvis.VoiceBridge` is a console app for the Mac (it also runs on Linux): it only listens for
"Hey Jarvis" locally and carries sound, everything else happens on the server.

```bash
dotnet run --project src/Jarvis/Jarvis.VoiceBridge -- --server https://home-apps.dev --device mac-biuro
# dev: --server https://app.localhost --insecure
```

- **First run:** downloads the three openWakeWord models (v0.5.1, checked by hash) into `~/.homeapps/voice-bridge`,
  macOS asks for microphone access for the terminal, and the browser opens HomeApps' consent screen ("Zezwól"). The
  bridge then keeps its refresh token in the Keychain and appears under "Połączone aplikacje" as "JARVIS: most głosowy".
  `--logout` forgets it on this Mac.
- **Using it:** "Hey Jarvis" (a rising chime) → say the request → after ~0.7 s of quiet a falling chime, the transcript
  and JARVIS's answer are printed and spoken (`afplay`). When he asks to confirm, a chime and 8 s for "tak" / "nie"
  without the wake word. Enter starts without the wake word, ends the utterance early, or stops JARVIS mid-answer;
  `q` quits. While he speaks the microphone is ignored (a Mac has no echo cancellation here), so press Enter to interrupt him.
- **Tuning:** `--threshold` (0.5) is the wake word's score. `--save-recordings <folder>` keeps each turn as a WAV on
  this Mac only (V7); `--check-wake <file.wav>` prints the best score in a 16 kHz recording, e.g. one of yours, to pick a
  threshold. Synthetic English "hey jarvis" scores 0.96-0.99, unrelated speech 0.00; a Polish accent may score lower.
- **Licence:** the "hey jarvis" model is CC BY-NC-SA 4.0 (non-commercial), fine for a home.

## Production

`compose.prod.yaml` runs JARVIS next to the other services (database `homeapps_jarvis`, migrated on start). In `.env`:

| Variable | |
|---|---|
| `JARVIS_CLIENT_SECRET` | Required. The secret of Auth's `jarvis-service` client, which JARVIS asks delegated tokens with: `openssl rand -base64 32`. Both services read it from `.env`. |
| `JARVIS_CLAUDE_MODEL` | Optional. The model of JARVIS's answers: `haiku` (the cheapest, for tests), `sonnet`, `opus` or a full model id; empty = Claude Code's default. |
| `JARVIS_CLAUDE_CODE_OAUTH_TOKEN` | Optional. JARVIS's own answers run on the owner's Claude subscription: on any computer with Claude Code, `claude setup-token`, then paste the token here. Empty: the JARVIS page and `@Jarvis` answer "JARVIS jest teraz wyłączony", while the gateway still serves your own Claude Code. A secret. |
| `JARVIS_VOICE_URL` | Optional. JARVIS's ears and voice: the speech engine's URL as the server reaches it, e.g. Speaches on the Mac `http://192.168.1.20:8000` ("Voice" above). Empty: no voice. |

The compose file also gives Auth the `jarvis-service` client and `Auth__ConnectedApps__Resources__0` =
`https://${HOMEAPPS_DOMAIN}/jarvis/mcp`, so apps can sign in to the gateway. More settings, as environment variables of
the `jarvis` service: `Jarvis__DailyBudgetUsd` (2 USD a day for the whole home), `Jarvis__Enabled` (`false` switches the agent off), `Jarvis__ClaudeCode__MaxTurns`.

The tools: Households and Accounts always; plugins from `HOMEAPPS_PLUGINS` (the plugin's compose service must be named
after its id, as the plugins' own compose files do); HomeMix add-ons that declare `"jarvis": true` and were allowed
to use him when installed. Then the release demo: install Pogoda in a household, set its place, and ask on the JARVIS
page "Jarvis, jaka jutro pogoda?"; the answer comes from the `pogoda_forecast` tool.

`scripts/smoke.sh` checks the gateway (metadata, 401, `tools/list`, a tool call) and that a message gets an answer; with
`JARVIS_CLAUDE_CODE_OAUTH_TOKEN` set the answer is Claude's, otherwise the "switched off" line.

## Connecting Claude Code

Production: the gateway is `https://<your domain>/jarvis/mcp`, e.g. for home-apps.dev:

```bash
claude mcp add --transport http homeapps https://home-apps.dev/jarvis/mcp
```

Then in Claude Code `/mcp` → `homeapps` → Authenticate. The browser opens HomeApps' consent screen (sign in first if
needed); after "Zezwól" Claude Code has your tools, as you, with JARVIS's limits (reads accounts and households,
changes only plugin and add-on data; nothing asks for a confirmation there, Claude Code asks you itself). The app is
listed under "Połączone aplikacje" in the account settings, where you can disconnect it. claude.ai: Settings →
Connectors → Add custom connector, the same address.

### Dev

Run the stack with `--profile services` (or JARVIS from the IDE on `:5700`, routed by `dynamic/dev-routes.yaml`), then:

```bash
claude mcp add --transport http homeapps https://app.localhost/jarvis/mcp
```

In Claude Code, `/mcp` → `homeapps` → sign in: the browser opens HomeApps' consent screen (`/connect`), and the app
shows up under "Połączone aplikacje" in the account settings. claude.ai: Settings → Connectors → Add custom connector,
same address (it needs the gateway reachable from the internet, so production only).
