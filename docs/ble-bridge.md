# BLE bridge: a device that reaches the home through a phone

Status: **specification only.** There is no phone app yet; the firmware has a replaceable transport with a BLE stub
(`HOMEAPPS_TRANSPORT_BLE`, returns `ESP_ERR_NOT_SUPPORTED`). This page is the contract the later phone app and the BLE
transport implement. Frames and endpoints are those of [devices-protocol.md](devices-protocol.md).

## Idea in one paragraph

A tiny ESP32-C3 without Wi-Fi at its location (a garden sensor, a bag tag) talks Bluetooth LE to a phone. The phone is an
**untrusted relay**: it forwards bytes to the home and back, and can neither read nor forge them. To get that without
changing the server, the device keeps speaking the same HTTPS requests (`/devices/pair`, `/auth/devices/token`,
`/devices/report`) with the same device id, secret and tokens; only the bytes below TLS travel over BLE instead of Wi-Fi.

## Identity and trust: the phone is not trusted

- **End-to-end TLS.** The device runs its TLS client (mbedTLS, the same `HOMEAPPS_TLS_*` trust modes: bundle, pinned CA,
  TOFU) *through* the phone. The phone opens a plain TCP connection to the home's host:443 and copies the TLS records
  between that socket and the GATT stream. It sees only ciphertext, the server name (SNI) and sizes.
- Device id, `secret` (pairing answer) and the short-lived token never leave the device unencrypted. The secret is sent only
  inside TLS to `/auth/devices/token`; the phone cannot replay it. A malicious phone can drop, delay or cut a connection
  (denial of service), nothing more. It cannot make the device trust another server: the certificate check stays on the device.
- The phone is told where to connect by the user in the phone app (the same HomeApps address as in setup). The device does
  not trust that address: a wrong host fails the certificate check.
- **Pairing a device through BLE** uses the normal one-time code and descriptor (`POST /devices/pair`) inside the same
  tunnel; the code is entered in the device setup (see Setup below). The result is an ordinary paired device.
- Why not an application-level signature instead? The server has no signature scheme for frames (tokens come from the
  secret). Adding one would be a server change; tunnelling TLS needs none.

## Server side: nothing changes

The home sees ordinary HTTPS from the phone's IP: same endpoints, token, rate limits (per device token; pairing and token
endpoints are per client IP, so many devices behind one phone share the IP limit: `PairPerMinute` 10,
`DeviceTokenPerMinute` 600). Sleepy semantics apply to every BLE device: `sleepy: true`, no WebSocket,
`POST /devices/report`, `settings` in the answer. A device with a WebSocket link over BLE is **out of scope** (a phone
would have to stay connected; use report mode). No new endpoint, field or error code.

## GATT layout

One primary service, 128-bit UUIDs are placeholders to be allocated once (replace `xxxx` before the first release; never
change them afterwards).

| Item | UUID | Properties | Content |
| --- | --- | --- | --- |
| Service `HomeApps Bridge` | `HA00xxxx-0000-1000-8000-00805F9B34FB` (placeholder) | | advertised, with the device name |
| `Info` | `HA01xxxx-...` | Read (encrypted) | `{"v":1,"proto":"homeapps.device/1","mtu":N,"model":"author/name"}` UTF-8 JSON, no secrets; optional `"fingerprint"` (TOFU only) is the server certificate SHA-256 the device saw, for the app to show |
| `Up` (device to phone) | `HA02xxxx-...` | Notify (encrypted) | chunks of the device's TLS byte stream |
| `Down` (phone to device) | `HA03xxxx-...` | Write without response (encrypted) | chunks of the server's TLS byte stream |
| `Control` | `HA04xxxx-...` | Write, Notify (encrypted) | connection control, below |

**Control messages** (1 type byte, then fields), all small:

| Type | Direction | Meaning |
| --- | --- | --- |
| `0x01 OPEN` | device to phone | open a TCP connection to the home (phone knows the address) |
| `0x02 OPENED` / `0x03 FAILED code` | phone to device | result (`1` no network, `2` refused, `3` timeout) |
| `0x04 CLOSE` | both | end the stream (the device does this after the response) |
| `0x05 WAKE` | phone to device | optional, a user asked for an immediate report |
| `0x06 CREDIT n` | device to phone | `n` more bytes of `Down` buffer, 2 bytes big-endian (type byte plus 2 = 3 bytes total) |
| `0x07 SETUP tlv` | phone to device | address, code and optional fingerprint as one TLV (type byte plus the TLV), only after bonding |

### MTU, chunking and framing

- Request ATT MTU 247 (payload 244 after the 3-byte header); use whatever is negotiated, minimum 23 (20 bytes).
  Data length extension on, 2M PHY off by default (range).
- `Up`/`Down` carry the **raw TLS stream** split in arbitrary chunks: no extra framing is needed, because TLS records and
  HTTP/1.1 (`Content-Length`) delimit themselves. Each chunk is one notification or write; the receiver appends.
- Flow control: Notify is paced by the controller (a queued notification at a time); on `Down` the phone must not write
  faster than the device's credit: the device sends a 2-byte credit (`0x06 CREDIT n`) on `Control` when it has `n` more bytes
  of buffer. Initial credit 2048.
- Limits mirror the protocol: a report is at most 50 frames and 64 KB; at MTU 244 that is ~270 chunks, so a `/report`
  with a few frames (about 1 KB) is 5 chunks plus the TLS handshake (about 5 to 7 KB with a typical chain, ~30 chunks).

## Pairing, bonding and the link security

- BLE pairing is **LE Secure Connections** with passkey entry or numeric comparison where the device has a display or
  button; a device with no I/O uses **Just Works plus the application-level check**: the phone app shows the model and name
  from `Info` and only then proceeds, and TLS to the home protects the data regardless of the BLE link.
- The link encryption protects against eavesdroppers in the room; the **security of the frames does not depend on it**
  (TLS does). That is why a Just Works device is acceptable.
- Bonding keys are stored in NVS (encrypted like the secret). **Factory reset or "back to setup" erases the bond**; the
  phone must forget the device too. A device accepts one bonded phone at a time (a second app must unpair first); this is
  a convenience limit, not a security one.
- Privacy: advertise with a resolvable private address after bonding; before bonding only the service UUID and a short name.

## Setup and pairing a device

1. The device has no Wi-Fi credentials but BLE is the selected transport: it advertises (up to `HOMEAPPS_SETUP_TIMEOUT_S`).
2. The phone app connects, shows `Info`, the user enters the HomeApps address and the pairing code (the app is the portal
   that the access-point page is for Wi-Fi devices; the values reach the device in a `Control` write of type `0x07 SETUP`
   with the same fields as the portal: address, code, optional fingerprint, in one TLV, sent only after bonding).
3. The device opens the tunnel and runs `/devices/pair`; the answer is stored as for Wi-Fi.

## The downlink for sleepy devices

There is no live downlink (a sleepy device is not connected). The existing mechanism is reused: a member sets a control
(`POST /devices/{id}/set`); the home keeps it **pending** and returns it in the `settings` of the next
`POST /devices/report` answer; the device applies it, then reports the value in a `state` frame (a pending entry is cleared
only when the reported value equals it). Over BLE nothing differs: the answer is just another HTTP response in the tunnel.
So "how often" reaches the device at its next wake. `WAKE` lets a nearby phone shorten the wait.

## Power compared with Wi-Fi

- A wake over BLE avoids Wi-Fi association and DHCP (typically 1 to 3 s at ~100 mA) and the radio is on for a short
  connection event schedule. But the **TLS handshake** costs the same CPU either way and over BLE the stream is slower
  (about 10 to 40 kB/s), so the radio is on for roughly 1 to 3 s as well. Expect the same order of energy per report, a clear
  win only when the Wi-Fi path would be slow or absent. **Measure with [c3-power-lab.md](c3-power-lab.md)** before promising
  a battery life.
- Reduce cost: TLS session resumption (tickets) saved in RTC memory shortens the handshake to ~1 KB; send fewer, bigger
  reports; connection interval 30 to 50 ms while transferring, then close at once.
- Advertising while waiting for a phone costs more than a report: a sleepy device advertises only for a few seconds at a
  wake (`HOMEAPPS_BLE_ADV_S`, planned) and, when no phone is near, queues frames (the existing queue keeps the newest 50)
  and sleeps.

## Failure modes

| Failure | Device behaviour |
| --- | --- |
| No phone in range (no connection in the advertise window) | keep the queue, go back to sleep, retry at the next wake |
| Phone disconnects mid-transfer | abort the TLS session, keep the frames queued, retry next wake |
| `FAILED` (no network on the phone, home unreachable) | same; do not retry hot |
| Server certificate does not match | like Wi-Fi: refuse; for TOFU report the fingerprint on `Info` for the app to show |
| `401 devices.device_unpaired` / 3 `invalid_credentials` | back to setup, erase the bond (as for Wi-Fi) |
| 429 / 5xx | honour `Retry-After` (30 s cap for sleepy), then sleep |
| Malicious phone replays old chunks | TLS rejects them (record MAC) |
| Two devices, one phone | the app serves one device connection at a time; the IP rate limits above still apply |

## What the later phone app must implement

1. Scan for the service UUID, connect, request MTU, bond (LE Secure Connections), show `Info` and let the user accept.
2. Run the setup step (`SETUP` write): HomeApps address, pairing code, optional certificate fingerprint.
3. A TCP relay: on `OPEN`, connect to the configured host:443; copy bytes both ways between the socket and `Up`/`Down`;
   respect `CREDIT`; on `CLOSE` or socket end, close the other side.
4. Background operation: iOS state restoration / Android foreground service, reconnect when a bonded device advertises.
5. Never parse, store or log the stream; no token handling; one configured host only (no open proxy).
6. UX: bond status per device, last report time (from the household's device page, not the phone), errors from `FAILED`.

## What the firmware side still needs (not in this stage)

The transport seam (`homeapps_transport_t` in `src/homeapps_internal.h`) takes `post_json`; the BLE implementation will
open the tunnel, run an `esp_transport` over `Up`/`Down` with NimBLE, and call the same `esp_http_client` code on top.
Setup over BLE and the bond handling in `homeapps_start` are also future work. The stub pulls in no BLE stack.
