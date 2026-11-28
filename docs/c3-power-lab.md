# C3 Power Lab: power modes of an ESP32-C3 weather board

Test app: `firmware/tests/c3-power-lab/` (reuses the `homeapps-device` component, no fork). It sends random
`temperature`, `humidity`, `pressure` (hPa) and `battery` every `POWERLAB_PERIOD_S` seconds (default 60) as a
`weather` + `power` device. The server accepts `pressure` (see `docs/device-capabilities.md`).
Status: **not build-tested locally** (no ESP-IDF); CI builds it on `esp32c3`.

## Modes

Pick one in `idf.py menuconfig` -> "C3 Power Lab" -> "Power mode":

| Mode | Behaviour |
| --- | --- |
| Deep sleep with report | Sleepy device: wakes, connects, one `POST /devices/report`, deep sleep for the period. |
| Modem sleep | Always connected (`/devices/link`), `WIFI_PS_MIN_MODEM`, a report every period. |
| No power saving | Always connected, `WIFI_PS_NONE`, a report every period. |

The serial log prints `report OK|FAIL wake_to_send_ms=<n>` for every cycle.

## Metrics table

Measured with the same board, firmware, period, router and distance for all three modes. Fill after the runs.

| Metric | How to get it | Deep sleep | Modem sleep | No saving |
| --- | --- | --- | --- | --- |
| Average current (mA) | PPK2 average over >= 1 h | pending | pending | pending |
| Wake-to-send time (ms) | `wake_to_send_ms` in the log, median | pending | pending | pending |
| Successful reports (%) | OK reports / expected reports over 24 h (device log vs. device card history) | pending | pending | pending |
| Recovery after Wi-Fi loss (s) | Switch the AP off for 5 min, time until the next report is shown | pending | pending | pending |
| Times home showed "silent" | Count of silent notifications/status changes during the 24 h run | pending | pending | pending |

## PPK2 procedure

1. Use a bare C3 board or one where the USB diode/regulator can be bypassed: do not measure through the USB port
   (the converter and LDO add their own current).
2. Set the PPK2 to source mode at 3.3 V and feed the board's 3.3V rail directly (PPK2 VOUT to 3V3, GND to GND),
   with nothing else powering the board. Flash first over USB, then disconnect USB.
3. Record at least one full cycle in each mode (deep sleep: several periods, to include the wake peak). Take the
   average over >= 1 h.
4. Keep the period identical across modes and note RSSI. Repeat the Wi-Fi loss test per mode.

## Recommendation for battery devices

**Pending real measurements.** No numbers have been measured yet; do not choose a mode from this page until the
table above is filled in.

## Baseline: phone hotspot (no firmware change)

With no home Wi-Fi, enable a phone hotspot (2.4 GHz) and enter its name and password in the board's setup page
(`HomeApps-XXXX`, `192.168.4.1`). The same binary pairs and reports through the hotspot: no rebuild, no code change.
Pass when the device card in the JARVIS tab shows the readings and `battery`, and the device is marked silent after
2.5 missed periods when the hotspot is switched off.
