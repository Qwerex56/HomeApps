#pragma once
/* homeapps-device: connect an ESP32 (S3, C3, C6) to HomeApps. Protocol: docs/devices-protocol.md. */
#include <stdbool.h>
#include <stddef.h>
#include "esp_err.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Limits the component enforces (protocol): 50 items per list, description 64 000 characters, action names. */
#define HOMEAPPS_MAX_ITEMS 50
#define HOMEAPPS_MAX_DESCRIPTOR_CHARS 64000
#define HOMEAPPS_MAX_FRAMES_PER_MINUTE 120
#define HOMEAPPS_MAX_REPORT_FRAMES 50
#define HOMEAPPS_MAX_REPORT_BYTES (64 * 1024)

/* A control, sensor, event or action. Unused members stay NULL. schema_json is a JSON Schema ("{\"type\":\"boolean\"}"),
 * for an action it is the input schema (type object). widget: switch, slider, select, button (controls). */
typedef struct {
    const char *name;        /* ^[a-z][a-z0-9_]{0,19}$ for actions; lowercase letters, digits, _ otherwise */
    const char *label;
    const char *description;
    const char *schema_json;
    const char *widget;
    const char *unit;
    bool notify;             /* events */
    bool location;           /* events */
    bool read_only;          /* actions */
    bool confirm;            /* actions */
} homeapps_item_t;

typedef struct {
    const char *model;               /* "author/name" */
    const char *version;
    const char *purpose;             /* "smart_home" or "assistant" */
    const char *const *roles;        /* NULL-terminated */
    const char *capabilities_json;   /* JSON array, e.g. [{"type":"switch"}] ; NULL = none */
    const char *jarvis;              /* up to 500 characters, data for JARVIS */
    int heartbeat_s;                 /* 0 = protocol default; 5..86400 */
    bool sleepy;                     /* no link: use homeapps_flush() (POST /devices/report) */
    const homeapps_item_t *controls; size_t controls_len;
    const homeapps_item_t *sensors;  size_t sensors_len;
    const homeapps_item_t *events;   size_t events_len;
    const homeapps_item_t *actions;  size_t actions_len;
} homeapps_descriptor_t;

/* A validated command from the server. Return ESP_OK to answer set_result ok, else the text is sent. */
typedef esp_err_t (*homeapps_set_cb_t)(const char *name, const char *value_json, void *user);

typedef struct {
    const homeapps_descriptor_t *descriptor;
    homeapps_set_cb_t on_set;
    void (*on_setup_password)(const char *ssid, const char *password, void *user); /* show it on a display */
    void *user;
} homeapps_config_t;

/* Checks the descriptor against the protocol limits (also done by homeapps_start). */
esp_err_t homeapps_validate(const homeapps_descriptor_t *d, char *err, size_t err_len);

/* Starts everything: setup portal when unconfigured, then Wi-Fi, SNTP, pairing, link. Returns once started
 * (for a sleepy device: once connected, ready for homeapps_send_* and homeapps_flush).
 * A sleepy device gives up instead of waiting forever (Wi-Fi 30 s, pairing 4 attempts): it returns ESP_ERR_TIMEOUT and the
 * caller should go back to deep sleep and try at the next wake. */ */
esp_err_t homeapps_start(const homeapps_config_t *config);

/* Frames. With a link they go out at once (120 per minute), for a sleepy device they are queued for homeapps_flush(). */
esp_err_t homeapps_send_state(const char *values_json);                      /* {"temperature":21.5} */
esp_err_t homeapps_send_event(const char *name, const char *data_json);      /* data_json may be NULL; "at" only after SNTP */
esp_err_t homeapps_send_status(int battery_percent, float voltage, const char *error); /* battery < 0 = omit */

/* Sleepy devices: fresh token, one POST /devices/report with the queued frames (ESP_OK when the server took them). */
esp_err_t homeapps_flush(void);

/* Re-opens setup / factory resets (also on the button, see Kconfig). */
void homeapps_open_setup(void);
void homeapps_factory_reset(void);

#ifdef __cplusplus
}
#endif
