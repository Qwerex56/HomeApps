#pragma once
#include "homeapps_device.h"
#include "cJSON.h"

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

/* setup portal: blocks until the user saved a configuration or the timeout hit; returns ESP_OK when saved */
esp_err_t homeapps_setup_run(const char *fingerprint_hint, void (*on_password)(const char *, const char *, void *), void *user);
