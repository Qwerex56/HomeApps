#include <string.h>
#include "nvs_flash.h"
#include "nvs.h"
#include "homeapps_internal.h"

#define NS "homeapps"

esp_err_t homeapps_store_load(homeapps_store_t *s)
{
    memset(s, 0, sizeof(*s));
    nvs_handle_t h;
    esp_err_t e = nvs_open(NS, NVS_READONLY, &h);
    if (e != ESP_OK) return e;
    size_t n;
#define LOAD(field, key) n = sizeof(s->field); nvs_get_str(h, key, s->field, &n)
    LOAD(ssid, "ssid"); LOAD(wifi_password, "wpass"); LOAD(base_url, "url"); LOAD(code, "code");
    LOAD(device_id, "devid"); LOAD(secret, "secret"); LOAD(fingerprint, "fp");
#undef LOAD
    nvs_close(h);
    return ESP_OK;
}

esp_err_t homeapps_store_save(const homeapps_store_t *s)
{
    nvs_handle_t h;
    esp_err_t e = nvs_open(NS, NVS_READWRITE, &h);
    if (e != ESP_OK) return e;
    nvs_set_str(h, "ssid", s->ssid); nvs_set_str(h, "wpass", s->wifi_password); nvs_set_str(h, "url", s->base_url);
    nvs_set_str(h, "code", s->code); nvs_set_str(h, "devid", s->device_id); nvs_set_str(h, "secret", s->secret);
    nvs_set_str(h, "fp", s->fingerprint);
    e = nvs_commit(h);
    nvs_close(h);
    return e;
}

void homeapps_store_erase_all(void)
{
    nvs_handle_t h;
    if (nvs_open(NS, NVS_READWRITE, &h) == ESP_OK) { nvs_erase_all(h); nvs_commit(h); nvs_close(h); }
}

void homeapps_store_erase_credentials(void)
{
    /* Factory reset and "unpaired" both drop Wi-Fi, secret and pairing state; the AP password is kept elsewhere. */
    homeapps_store_t s;
    homeapps_store_load(&s);
    homeapps_store_t empty = {0};
    strlcpy(empty.base_url, s.base_url, sizeof(empty.base_url)); /* the address is a convenience for the form */
    homeapps_store_save(&empty);
}
