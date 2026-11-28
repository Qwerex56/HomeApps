#pragma once
#include "homeapps_device.h"
#include "cJSON.h"

/* A bool Kconfig option is undefined (not 0) when off: always go through this. */
#ifdef CONFIG_HOMEAPPS_INSECURE_HTTP
#define HOMEAPPS_INSECURE 1
#else
#define HOMEAPPS_INSECURE 0
#endif

typedef struct {
    char ssid[33];
    char wifi_password[65];
    char base_url[128];   /* https://home.example.com */
    char code[16];        /* pairing code, cleared after pairing */
    char device_id[40];
    char secret[128];
    char fingerprint[96]; /* TOFU: pinned SHA-256 of the server certificate (hex) */
} homeapps_store_t;

/* store (NVS) */
esp_err_t homeapps_store_load(homeapps_store_t *s);
esp_err_t homeapps_store_save(const homeapps_store_t *s);
void homeapps_store_erase_all(void);
void homeapps_store_erase_credentials(void); /* back to setup: keeps nothing of wifi/secret */

/* descriptor */
char *homeapps_descriptor_to_json(const homeapps_descriptor_t *d); /* caller frees */

/* setup portal: starts from the saved store (prefills SSID, address, fingerprint; keeps secret and password unless replaced). Blocks until the user saved a configuration or the timeout hit; returns ESP_OK when saved */
esp_err_t homeapps_setup_run(const homeapps_store_t *current, void (*on_password)(const char *, const char *, void *), void *user);

/* transport seam: how the three request/response calls (pair, token, report) reach the home. The default is Wi-Fi + HTTPS
 * (homeapps_device.c); BLE is a stub, see docs/ble-bridge.md. The WebSocket link of a non-sleepy device stays on Wi-Fi. */
typedef struct { char *body; size_t len; size_t cap; int retry_after; } http_out_t;

typedef struct {
    const char *name;
    esp_err_t (*open)(void);   /* ESP_OK, or the reason the transport cannot be used (ESP_ERR_NOT_SUPPORTED) */
    /* POST json to the home; returns the HTTP status or -1. Fills out->body (up to out->cap) and out->retry_after. */
    int (*post_json)(const char *path, const char *bearer, const char *body, http_out_t *out);
} homeapps_transport_t;

#ifdef CONFIG_HOMEAPPS_TRANSPORT_BLE
extern const homeapps_transport_t homeapps_transport_ble;
#endif
