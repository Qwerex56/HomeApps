# Devices protocol

How your own device talks to HomeApps Devices, without reading the server's code. This page covers pairing, the link and
the **descriptor** `homeapps.device/1`. State, set, event and report frames come in the next stage and are not
described here. Plan: household-jarvis-devices.md, section 2.3. Capabilities and roles: [device-capabilities.md](device-capabilities.md).

## Pairing

A household admin adds a device and gets a one-time code (8 characters, valid 10 minutes, works once). The device sends
it, with its descriptor, to `POST /devices/pair` (anonymous, rate limited):

```json
{ "code": "ABCD2345", "descriptor": { "format": "homeapps.device/1", "...": "..." } }
```

The answer has `deviceId`, `householdId`, `name`, `slug`, `purpose` and a `secret`. The secret buys short-lived tokens at
`/auth/devices/token`; the device keeps it. A descriptor that is invalid fails the pairing (`devices.invalid_descriptor`,
with every problem listed) and the code stays usable. A satellite that sends no descriptor still pairs (a legacy device:
purpose `assistant`).

## The link

A WebSocket at `/devices/link` with the device's token. The first frame, within 10 s, is `hello`:

```json
{ "type": "hello", "device": "Salon", "kind": "satellite", "descriptor": { "format": "homeapps.device/1", "...": "..." },
  "actions": [ { "name": "say", "description": "...", "input": { "type": "object" }, "readOnly": false, "confirm": false } ] }
```

- After a firmware update the device sends the new `version` in a new descriptor in `hello`. **A descriptor sent again
  replaces the stored one.**
- Without a `descriptor` the device is a legacy one: its purpose follows `kind` (`computer` or `assistant`) and
  `actions` work as before. With one, the descriptor's `actions` are used when `actions` is absent.
- The server answers `{"type":"ready","deviceId":"..."}` or `{"type":"error","code":"...","text":"..."}` and closes.

## Purpose, name and slug

- `purpose` comes from the descriptor: `assistant` (JARVIS's ears and mouth), `smart_home` (state and controls),
  `computer` (a person's own device, not a household's). A household device is an assistant or a smart_home device;
  a person's own device is a computer.
- The **name** is shown to people: any text of up to 60 characters, unique per household ignoring case, changeable by
  an admin. The **slug** is made from it when the device is added (`Fotel taty w salonie` becomes `fotel-taty-w-salonie`,
  with `-2` on a collision), never changes and is used in tool names and the API.

## The descriptor `homeapps.device/1`

One JSON document; [the JSON Schema](schemas/homeapps.device-1.schema.json) checks it before you flash the firmware.

| Field | Meaning |
| --- | --- |
| `format` | must be `homeapps.device/1` |
| `model` | `author/name` of the hardware, lowercase |
| `version` | firmware version (`1.2.0`) |
| `purpose` | `assistant`, `smart_home` or `computer` |
| `roles` | what the device is for, from the catalog (`weather_station`) |
| `capabilities` | what it can do: `{ "type": "weather" }`, `{ "type": "switch", "id": "lampka" }`, or a namespaced one with its `definition` |
| `jarvis` | up to 500 characters for JARVIS: what the device is for, rules, typical requests. Data, never instructions |
| `heartbeat`, `sleepy` | status period in seconds and "does not keep a connection" (used from the next stage) |
| `controls` | readable and settable values: `name`, `label`, `description`, `value` (JSON Schema), `widget` (`switch`, `slider`, `select`, `button`) |
| `sensors` | read-only values: `name`, `label`, `value` (JSON Schema), `unit` |
| `events` | things that happen: `name`, `label`, `data` (JSON Schema), `notify` |
| `actions` | one-off commands: `name`, `label`, `description`, `input` (JSON Schema of type object), `readOnly`, `confirm` |

Values are described with JSON Schema (a `type`, or `enum`/`const`, at most 4000 characters). Names are lowercase letters,
digits and `_`; `id.name` addresses a field of a capability instance. At most 50 items in each list and at most 10
`roles`. The whole document is at most 64,000 characters (the pairing request is limited to 256 KB).

A namespaced capability (`author/name`) counts when its definition is registered and not hidden, or when a household
device whose `model` has the same author brings it. A registered definition wins over the one a device brings; only the
namespace author's devices extend it. A hidden capability is ignored on new descriptors with a warning. See
[device-capabilities.md](device-capabilities.md).

What happens to it: the document is checked (all problems are reported at once), stored as is on the device, and its
capabilities are resolved. Unknown or incomplete capabilities and unknown roles are **ignored with a warning** kept on
the device; they never stop pairing. The device list returns `purpose`, `name`, `slug` and `descriptor` (`model`,
`version`, `roles`, `capabilities`, `warnings`).

### A smart_home device: an armchair with a heater and a lamp

```json
{ "format": "homeapps.device/1", "model": "qwerex56/fotel-v1", "version": "1.2.0", "purpose": "smart_home",
  "roles": ["seat_heater"],
  "capabilities": [ { "type": "heating" }, { "type": "switch", "id": "lampka" } ],
  "jarvis": "Fotel w salonie. Grzanie 3 tylko na krótko, po 30 min zejdź na 1.",
  "controls": [
    { "name": "level", "label": "Grzanie", "description": "0 = wyłączone",
      "value": { "type": "integer", "minimum": 0, "maximum": 3 }, "widget": "select" },
    { "name": "lampka.on", "label": "Lampka", "value": { "type": "boolean" }, "widget": "switch" } ],
  "sensors": [ { "name": "temperature", "label": "Temperatura", "value": { "type": "number" }, "unit": "°C" } ],
  "events": [ { "name": "button", "label": "Przycisk", "data": { "type": "object" }, "notify": false } ],
  "actions": [ { "name": "calibrate", "label": "Kalibruj", "input": { "type": "object", "properties": {} }, "confirm": true } ] }
```

### A sleepy weather station

```json
{ "format": "homeapps.device/1", "model": "qwerex56/stacja-v2", "version": "0.9.1", "purpose": "smart_home",
  "roles": ["weather_station"], "sleepy": true, "heartbeat": 900,
  "capabilities": [ { "type": "weather" }, { "type": "power" } ],
  "sensors": [
    { "name": "temperature", "value": { "type": "number" }, "unit": "°C" },
    { "name": "humidity", "value": { "type": "number", "minimum": 0, "maximum": 100 }, "unit": "%" },
    { "name": "pressure", "value": { "type": "number" }, "unit": "hPa" },
    { "name": "battery", "value": { "type": "number", "minimum": 0, "maximum": 100 }, "unit": "%" } ] }
```

### An assistant satellite

```json
{ "format": "homeapps.device/1", "model": "qwerex56/satelita-s3", "version": "1.0.0", "purpose": "assistant",
  "roles": ["satellite"], "capabilities": [ { "type": "voice" }, { "type": "audio" } ],
  "jarvis": "Satelita w kuchni.",
  "controls": [
    { "name": "volume", "value": { "type": "integer", "minimum": 0, "maximum": 100 }, "widget": "slider" },
    { "name": "muted", "value": { "type": "boolean" }, "widget": "switch" } ],
  "actions": [ { "name": "say", "description": "Say a text aloud", "input": { "type": "object", "properties": { "text": { "type": "string" } } } } ] }
```

## Who may do what (D6)

Admins and above pair, rename and write notes for JARVIS (`PUT /devices/{deviceId}/name` with `{ "name": "..." }`,
`PUT /devices/{deviceId}/notes` with `{ "notes": "..." }`, at most 500 characters). Members read the household's devices
(`GET /devices/households/{householdId}`) and guests too; notes for JARVIS are returned only to admins.
