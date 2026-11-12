# 0009: HomeMix add-ons in a sandbox, next to server plugins

Status: accepted (2026-11-12). Plan and decisions W1-W16: `plans/features/household-apps-upload.md`.

## Context

Plugins ([plugins.md](../plugins.md)) are services started with the server: their own repository, container, database
and MCP server, listed in `HOMEAPPS_PLUGINS`. They are the extension framework for people who host HomeApps, and they
stay as they are. But a household or a person cannot add anything without access to the server, and a plugin's UI
runs in the app's own origin with the person's token (`useHost().accessToken()`), so letting a household admin upload
one would let them act as anyone who opens that household, the instance Owner included.

## Decision

- Two words, two things: a **plugin** is added with the server start (compose, `.env`, the Owner); a **HomeMix**
  (add-on) is added from the web app, to one household (role Admin and up) or to one account (its owner). A HomeMix
  never applies to the whole instance.
- A HomeMix is **front end only**: a zip with `homemix.json` and static files, installed from a GitHub repository link
  or an uploaded zip. It runs in an iframe whose response carries `Content-Security-Policy: sandbox allow-scripts ...`,
  so its origin is opaque even when the URL is opened directly: no cookies, no storage, no access to the app window.
- It talks to HomeApps only through a **bridge** (`postMessage`) that offers named capabilities the installer agreed
  to. The bridge never hands out tokens, emails, server roles, other households' or installations' data, instance
  configuration, or calls to core APIs on the person's behalf.
- Its data lives on the server in the new core service **HomeMix** (`src/HomeMix`), in a space of its own per
  installation; it only reads core data, through the bridge. External network only to the `https://` origins listed
  in `homemix.json` and accepted at install (CSP `connect-src`).
- JARVIS reaches a HomeMix through plain MCP: the HomeMix service is an MCP server whose tools come from declarations
  in `homemix.json` or from `tools/*.js` scripts run in Jint with the same `ctx` as the bridge, with time and memory
  limits. This is the only add-on code that runs on the server, and it has nothing but `ctx`.
- The store is only an aggregator of GitHub: an index of repository links; publishing is linking a repository. Nothing
  is uploaded to the store; the server keeps only installed packages, one copy per SHA-256.

## Consequences

- Households and accounts can be extended without touching the server, safely; a host who wants none of it sets
  `HomeMix:UploadsEnabled=false` or blocks an add-on by id or hash.
- An add-on has no backend of its own and no integration messages; anything bigger is still a plugin.
- A capability added to the bridge needs a test that nothing on the "never" list leaks.
- The HomeMix service is a core service: the assistant guard refuses changes from tokens with `act` by default; data
  endpoints and `/mcp` opt in with `.AllowAssistant()`, installing and removing stay refused.
