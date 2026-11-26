# HomeApps docs

Start here. Descriptions say how things are, not which stage built them.

## Reading path

1. [../README.md](../README.md): what HomeApps is.
2. [deployment.md](deployment.md): put it on a server, back it up, update it (including [2.4 to 3.0](deployment.md#upgrading-from-24-to-30)).
3. [development.md](development.md): local setup with plugins; [working-with-the-repo.md](working-with-the-repo.md): build, test, branches, CI, releases.
4. [how-a-service-is-built.md](how-a-service-is-built.md): the backend recipe, including API versioning (`/v1`); [adr/](adr/README.md): decisions.

## Where to find what

| Topic | Doc |
| --- | --- |
| Accounts, sign-in, households, notifications | [auth.md](auth.md), [accounts.md](accounts.md), [households.md](households.md), [notifications.md](notifications.md) |
| Devices (registry, capabilities, wire protocol) | [devices.md](devices.md), [device-capabilities.md](device-capabilities.md), [devices-protocol.md](devices-protocol.md) |
| JARVIS (household assistant, voice, MCP) | [jarvis.md](jarvis.md), [mcp.md](mcp.md) |
| HomeMix add-ons | [homemix.md](homemix.md), [homemix-authoring.md](homemix-authoring.md) |
| Plugins in their own repos | [plugins.md](plugins.md), [plugin-todo.md](plugin-todo.md), [plugin-chat.md](plugin-chat.md) |
| Web app | [web.md](web.md) |
| API versioning | [how-a-service-is-built.md](how-a-service-is-built.md#api-versioning) |
| Releases and updating | [working-with-the-repo.md](working-with-the-repo.md#releases), [deployment.md](deployment.md#updating), [../CHANGELOG.md](../CHANGELOG.md) |

Repositories `HomeApps.*` link here instead of copying these pages.
