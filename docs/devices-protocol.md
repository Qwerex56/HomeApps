# Devices protocol

How your own device talks to HomeApps Devices, without reading the server's code. This page covers pairing, the link and
the **descriptor** `homeapps.device/1`, the **frames** (`state`, `set`, `event`, `status`), `POST /devices/report` for
devices that sleep and the heartbeat rules. Plan: household-jarvis-devices.md, section 2.3. Capabilities and roles: [device-capabilities.md](device-capabilities.md).

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
| `heartbeat`, `sleepy` | status period in seconds (5 to 86400) and "does not keep a connection" (see [Heartbeat](#heartbeat)) |
| `controls` | readable and settable values: `name`, `label`, `description`, `value` (JSON Schema), `widget` (`switch`, `slider`, `select`, `button`) |
| `sensors` | read-only values: `name`, `label`, `value` (JSON Schema), `unit` |
| `events` | things that happen: `name`, `label`, `data` (JSON Schema), `notify`, `location` (the event says where something is, see [Events](#events)) |
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
    { "name": "battery", "value": { "type": "number", "minimum": 0, "maximum": 100 }, "unit": "%" } ],
  "events": [ { "name": "motion", "label": "Ruch" } ] }
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

## Frames

After `ready` the device and the server exchange JSON frames on the link. Every value is checked against the device's own
descriptor, lightly: `type`, `enum`/`const`, `minimum`/`maximum`, `minLength`/`maxLength`, an object's `required` and
`properties`. Nothing else in a schema is enforced. A frame that fails is **refused as a whole** and changes nothing; the
server answers it with `{"type":"error","code":"devices.invalid_frame","text":"..."}` (every problem listed) and the
connection stays open. A frame the server accepts gets no answer.

Only a paired household device that sent a descriptor may send these frames (`devices.frames_unsupported` otherwise).

### `state`: the current values

```json
{ "type": "state", "values": { "level": 2, "temperature": 21.5 } }
```

Send it after connecting and on every change. Each name must be a control or a sensor of the descriptor and its value
must fit that schema. A new value replaces the old one; names the frame does not mention keep their last value. Devices
stores the values with the time it received them (not a history; that is for events).

### `set`: change a control (server to device)

```json
{ "type": "set", "id": "7f3c...", "name": "level", "value": 3 }
```

A household member (not a guest, D6) changes a control with `POST /devices/{id}/set` ([devices.md](devices.md)). The
server checks `value` against the control's schema first; the device never gets a value that breaks it. The device answers
within `Devices:ActionTimeout` (20 s) with `{ "type": "set_result", "id": "7f3c...", "ok": true }` (or `"ok": false` and a
`text` with the reason) and then sends the new `state`. A device that is not connected is offline (409
`devices.offline`): **commands are not queued** for a sleepy device, so a sleepy device's controls are not settable for now.

### `event`: something happened

```json
{ "type": "event", "name": "button", "at": "2026-11-18T19:02:11Z", "data": { "count": 2 } }
```

`name` must be declared in the descriptor's `events`; `data` (optional) must fit the event's `data` schema and be at most
4000 characters. `at` is when it happened (ISO 8601); a missing, unreadable or future time becomes the time the server
received it. Devices stores it in the event history, publishes the contract `DeviceEventOccurred` on RabbitMQ and sends it
to the household's open streams (members only). An event declared with `"location": true`, or any event of a device with
the `geo` capability, is a **location event**: kept shorter (see below) and never shown to guests.

Retention (D9) is the household's setting in the JARVIS tab: events are kept `eventDays` (default 30), location events
`locationEventDays` (default 7). Devices reads it from Households and a daily job deletes what is older.

### `status`: the heartbeat

```json
{ "type": "status", "battery": 78, "voltage": 3.9, "rssi": -61, "uptime": 86400, "firmware": "1.2.0", "error": null }
```

All fields except `type` are optional. `battery` is a percentage (0 to 100), `voltage` in volts, `rssi` in dBm (-200 to
0), `uptime` in seconds, `firmware` at most 50 and `error` at most 200 characters (`null` or absent: no error).

## Heartbeat

A `smart_home` or `assistant` device with a descriptor owes a `status` frame every **period**: `heartbeat` seconds from the
descriptor; when it is absent, 60 s, or 900 s for a device that declares `"sleepy": true`. A sleepy device should
declare its wake interval as `heartbeat`.

- Devices remembers the last report (battery, voltage, signal, uptime, firmware, error) with its time.
- When no `status` arrives for **2.5 periods** the device is **silent** (missing). That is different from offline, which
  only means the link is closed. Devices publishes `DeviceWentSilent` and notifies the household's admins (Admin and
  Owner), **once**. The next `status` ends the state. A device that never reported counts from its pairing. Time before
  a Devices restart does not count, so a restart does not make every device silent.
- A battery below **20%** notifies the admins once; the next notice needs the battery to have been at 25% or more in
  between (a 5 point hysteresis). A report without `battery` changes nothing.
- The first `status` after silence notifies the admins once more (silent notice, "the device reports again").
- Only `status` counts as a heartbeat, not `state` or `event`. Devices are scanned every 30 s, so the notice comes up to
  30 s late.

## `POST /devices/report`: devices that sleep

A battery device keeps no WebSocket: it wakes, sends the same frames in one request and sleeps again. Same device token
(`Authorization: Bearer`), body an array of frames:

```json
[
  { "type": "state", "values": { "temperature": 21.5, "humidity": 48 } },
  { "type": "event", "name": "motion", "at": "2026-11-18T19:02:11Z" },
  { "type": "status", "battery": 61, "rssi": -70 }
]
```

The answer is `200` with one result per frame, in order:
`[{ "ok": true }, { "ok": false, "code": "devices.invalid_frame", "text": "..." }, ...]`. A bad frame does not stop the
others. At most **50 frames** and **64 KB** per request (400 / 413 above that). A token that is not a paired household
device with a descriptor gets `devices.frames_unsupported`.

**Limits.** At most **60 requests per minute** per device token (`429` with `Retry-After` above that). On the WebSocket, at
most **120 frames per minute** per connection: a frame above that is dropped and answered with
`{"type":"error","code":"devices.rate_limited"}`; the connection stays. Payloads containing a NUL (`\u0000`) are refused as
`devices.invalid_frame`. If a frame cannot be stored, the WebSocket answers `devices.frame_failed` (connection stays) and
`/devices/report` answers `409`.

## Who may do what (D6)

Admins and above pair, rename and write notes for JARVIS (`PUT /devices/{deviceId}/name` with `{ "name": "..." }`,
`PUT /devices/{deviceId}/notes` with `{ "notes": "..." }`, at most 500 characters). Members read the household's devices
(`GET /devices/households/{householdId}`) and guests too; notes for JARVIS are returned only to admins.

Members read a device's state (`GET /devices/{id}/state`) and its event history (`GET /devices/{id}/events`) and set
controls (`POST /devices/{id}/set`); a guest reads state only, never events (D9). Details in [devices.md](devices.md).
