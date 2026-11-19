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
