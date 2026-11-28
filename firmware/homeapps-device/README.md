# homeapps-device

ESP-IDF component (ESP32-S3, C3, C6; IDF 5.2+) that connects a device to HomeApps. Protocol: `docs/devices-protocol.md`,
capabilities: `docs/device-capabilities.md`.

```c
homeapps_config_t cfg = { .descriptor = &desc, .on_set = on_set };
homeapps_start(&cfg);                       // setup portal, Wi-Fi, SNTP, pairing, link
homeapps_send_state("{\"on\":true}");
homeapps_send_event("pressed", "{\"kind\":\"single\"}");
```

Declare controls, sensors and events in a `homeapps_descriptor_t` (see `firmware/examples/`). `homeapps_validate` checks
the protocol limits (50 items per list, 64 000 characters, action names `^[a-z][a-z0-9_]{0,19}$`); the component also
keeps to 120 frames/min on the link, 50 frames and 64 KB per `/devices/report`.

## Setup (first start)

The device opens a WPA2 access point `HomeApps-XXXX` with a **random password** made on the first start (printed on the
serial log, handed to `on_setup_password` for a display; Kconfig can override it). Open `http://192.168.4.1`: 2.4 GHz
network, password, HomeApps address, pairing code. The portal closes on success or after 10 minutes. With
`CONFIG_HOMEAPPS_RESET_BUTTON_GPIO` set: hold 2 s to reopen setup, hold 5 s for a factory reset (erases secret and Wi-Fi).

## Secrets at rest

The device secret and the Wi-Fi password live in NVS. The examples ship `sdkconfig.defaults` with NVS encryption
(`CONFIG_NVS_ENCRYPTION`, key protected by HMAC with eFuse key block 0). **Burning the eFuse key block is irreversible**:
the key block cannot be reused or changed afterwards, so try it on a spare board first. Flash encryption and secure boot
are documentation only here: this component neither enables nor needs them, but they are the way to protect a device
that can be taken apart.

## TLS

Default: the Mozilla bundle (`esp_crt_bundle`). Kconfig alternatives: a pinned CA (`pinned_ca.pem`) or trust on first
use (the SHA-256 of the server certificate is pinned on the first connection, or typed into the setup page; check it
against the serial log). Plain http only with `HOMEAPPS_INSECURE_HTTP`: the pairing answer carries the device secret.

TOFU pins the leaf certificate: when the server certificate is renewed, reopen setup (hold the button 2 s) and clear or replace the fingerprint.

## Transport

Kconfig `HOMEAPPS_TRANSPORT`: `HOMEAPPS_TRANSPORT_WIFI` (default, Wi-Fi + HTTPS, unchanged) or `HOMEAPPS_TRANSPORT_BLE`, a **stub**
(`homeapps_start` returns `ESP_ERR_NOT_SUPPORTED`, no NimBLE linked). The seam is `homeapps_transport_t` in `src/homeapps_internal.h`
(`open`, `post_json` for pair, token and report). The phone-bridge design is in `docs/ble-bridge.md`.

## Behaviour

- A fresh token for every connection and every `/report`; `devices.device_unpaired` or 3 `invalid_credentials` in a row
  send the device back to setup; 429/5xx back off (honouring `Retry-After`).
- Events carry `at` only after SNTP; before that the server uses its receive time.
- `sleepy: true`: no link; `homeapps_start` gives up (`ESP_ERR_TIMEOUT`) when Wi-Fi or pairing does not work, then go back to deep sleep; `homeapps_send_*` queue frames and `homeapps_flush()` posts them.
- Descriptor `actions` are declared but this version does not deliver them to a callback yet.
- Not build-tested locally (no ESP-IDF): CI builds the examples (`.github/workflows/firmware.yml`).
