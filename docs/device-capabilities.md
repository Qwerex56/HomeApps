# Device capabilities and roles

A device tells HomeApps what it can do in its descriptor ([devices-protocol.md](devices-protocol.md)). A **capability**
is a fixed contract of field names and value schemas, so an add-on knows what to expect from any device that has it,
whoever made it. A **role** says what the device is for (`weather_station`, `seat_heater`). Plan: household-jarvis-devices.md,
2.5 and decisions D12 to D14.

The built-in catalog is data, not code: [`builtin-capabilities.json`](../src/Devices/Devices.Domain/Capabilities/builtin-capabilities.json)
is embedded in `Devices.Domain` and loaded at start. A new built-in capability or role is a pull request that changes that
file and this page, shipped in a release (D13).

## Built-in capabilities

A field is a *control* (read and set), a *sensor* (read only) or an *event* (recorded). "Needs" says what a device must
declare for the capability to count; otherwise it is ignored with a warning.

| Capability | Fields | Needs |
| --- | --- | --- |
| `power` | sensors `battery` (%), `voltage` (V), `source` (mains, battery, usb) | at least one |
| `weather` | sensors `temperature` (°C), `humidity` (%), `pressure` (hPa), optional `rain`, `wind` | at least one of temperature, humidity, pressure |
| `geo` | sensor `location` (object: lat, lon, accuracy), event `seen` (object: where) | both |
| `presence` | sensor `present` (boolean), optional event `motion` | `present` |
| `switch` | control `on` (boolean) | `on` |
| `level` | control `level` (integer 0 to 100) | `level` |
| `heating` | controls `level` (integer) or `target` (°C), sensor `temperature` | `level` or `target` |
| `button` | event `pressed` (object: kind single, double, long) | `pressed` |
| `audio` | controls `volume` (0 to 100), `muted` (boolean) | both |
| `voice` | speech through `/jarvis/voice`; no fields | nothing |

The declared value type must match (`integer` is accepted where the catalog says `number`). With several of one kind
the device gives an instance id: `{ "type": "switch", "id": "lampka" }` and its field is then `lampka.on`.

### Computer tools and Desktop contract (H-7)

Devices with `purpose: computer` (a person's own computer running HomeApps.Desktop or the voice bridge) connect over `/devices/link` and expose actions as MCP tools:
- **Built-in actions:** may declare `readOnly` (boolean) and `confirm` (boolean, whether confirmation is requested).
- **Local MCP servers proxy:** tools forwarded from local MCP servers running on the computer always enforce `readOnly = false` and `confirm = true`.
- **Action aliases:** names exposed to HomeApps must be ≤ 20 characters matching `^[a-z][a-z0-9_]{0,19}$`.
- Built-in capabilities on computers typically include `audio` (controls `volume`, `muted`) and actions like `say` or app control.


## Roles

`weather_station`, `seat_heater`, `tracker`, `doorbell`, `light`, `blind`, `thermostat`, `satellite`. A role is meaning
("what for"), a capability is the data contract ("what it can do"). An unknown role is ignored with a warning, like an
unknown capability.

## Your own capabilities (D14)

A capability with an author namespace, `qwerex56/akwarium`, needs no change in HomeApps: the device brings its
definition in the descriptor.

```json
{ "type": "qwerex56/akwarium", "definition": { "version": 1,
  "fields": [ { "kind": "sensor", "name": "ph", "value": { "type": "number" } },
              { "kind": "sensor", "name": "temp", "value": { "type": "number" }, "required": false } ] } }
```

- The first device that pairs with it registers it for the **whole instance** (not per household: it is a contract for
  store add-ons and must mean the same everywhere). It is registered only when the namespace author equals the author
  part of the descriptor's `model` (`qwerex56/fotel-v1` may register `qwerex56/akwarium`) **and** the device is a
  household device. Otherwise the capability is ignored with a warning.
- The model author is **self-declared**: nothing verifies it. This is first-come registration plus the Owner's power to
  hide, not real verification of who owns a namespace.
- Once registered, **the registered definition wins**: a device is checked against it, whatever definition it brings.
  Only a device whose model author is the namespace author can extend it: a newer `version` only **extends** it, fields
  it adds are kept (as optional), fields already registered do not change.
- A definition has at least 1 and at most 30 fields, a `version` from 1 to 10000, a `description` of at most 300
  characters and an `anyOf` of at most 30 field names. The whole descriptor is at most 64,000 characters and has at
  most 10 `roles`.
- A namespaced capability without a definition, a built-in name that is not in the catalog, or a capability whose fields
  the device does not declare is **ignored with a warning**. The device still pairs; the warning is stored on the device
  and shown with it (`warnings` in the device list).
- The server's Owner sees the registered ones at `GET /devices/admin/capabilities` and can hide or show them
  (`POST .../{author}/{name}/hide`, `.../unhide`). A **hidden** capability is ignored on new descriptors (with a
  warning on the device); devices already paired keep what they have until they send a descriptor again. Promoting one
  to a built-in is a release step.
