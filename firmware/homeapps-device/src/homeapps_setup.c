/* Setup portal: WPA2 access point with a random password (first start), one form, closes on success or timeout. */
#include <ctype.h>
#include <string.h>
#include <stdio.h>
#include "esp_log.h"
#include "esp_wifi.h"
#include "esp_netif.h"
#include "esp_random.h"
#include "esp_http_server.h"
#include "esp_timer.h"
#include "nvs.h"
#include "freertos/FreeRTOS.h"
#include "freertos/semphr.h"
#include "homeapps_internal.h"

static const char *TAG = "homeapps_setup";

static const char PAGE[] =
    "<!doctype html><html><head><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'>"
    "<title>HomeApps</title><style>body{font-family:sans-serif;max-width:26rem;margin:2rem auto;padding:0 1rem}"
    "input,button{width:100%;padding:.6rem;margin:.3rem 0 1rem;box-sizing:border-box}</style></head><body>"
    "<h1>HomeApps</h1><form method=post action=/save>"
    "<label>Wi-Fi network (2.4 GHz only)</label><input name=ssid required maxlength=32>"
    "<label>Wi-Fi password</label><input name=pass type=password maxlength=64>"
    "<label>HomeApps address</label><input name=url placeholder='https://home.example.com' required maxlength=127>"
    "<label>Pairing code</label><input name=code required maxlength=8 autocapitalize=characters>"
    "<label>Server certificate fingerprint (only for trust on first use, optional)</label><input name=fp maxlength=71>"
    "<button type=submit>Save</button></form></body></html>";

typedef struct { SemaphoreHandle_t done; homeapps_store_t cfg; bool saved; } portal_t;

static void url_decode(char *s)
{
    char *o = s;
    for (; *s; s++) {
        if (*s == '+') *o++ = ' ';
        else if (*s == '%' && isxdigit((unsigned char)s[1]) && isxdigit((unsigned char)s[2])) {
            char hex[3] = { s[1], s[2], 0 };
            *o++ = (char)strtol(hex, NULL, 16);
            s += 2;
        } else *o++ = *s;
    }
    *o = 0;
}

static void field(const char *body, const char *key, char *out, size_t cap)
{
    out[0] = 0;
    size_t kl = strlen(key);
    for (const char *p = body; p && *p;) {
        if (!strncmp(p, key, kl) && p[kl] == '=') {
            const char *v = p + kl + 1;
            const char *e = strchr(v, '&');
            size_t n = e ? (size_t)(e - v) : strlen(v);
            if (n >= cap) n = cap - 1;
            memcpy(out, v, n); out[n] = 0;
            url_decode(out);
            return;
        }
        p = strchr(p, '&');
        if (p) p++;
    }
}

static esp_err_t get_page(httpd_req_t *r)
{
    httpd_resp_set_type(r, "text/html");
    return httpd_resp_send(r, PAGE, HTTPD_RESP_USE_STRLEN);
}

static esp_err_t post_save(httpd_req_t *r)
{
    portal_t *p = r->user_ctx;
    char body[512];
    if (r->content_len >= sizeof(body)) { httpd_resp_send_err(r, HTTPD_400_BAD_REQUEST, "Too large"); return ESP_FAIL; }
    int got = httpd_req_recv(r, body, r->content_len);
    if (got <= 0) return ESP_FAIL;
    body[got] = 0;
    homeapps_store_t *c = &p->cfg;
    field(body, "ssid", c->ssid, sizeof(c->ssid));
    field(body, "pass", c->wifi_password, sizeof(c->wifi_password));
    field(body, "url", c->base_url, sizeof(c->base_url));
    field(body, "code", c->code, sizeof(c->code));
    field(body, "fp", c->fingerprint, sizeof(c->fingerprint));
    for (char *q = c->code; *q; q++) *q = (char)toupper((unsigned char)*q);
    size_t ul = strlen(c->base_url);
    while (ul && c->base_url[ul - 1] == '/') c->base_url[--ul] = 0;
    if (!c->ssid[0] || !c->base_url[0] || !c->code[0]) { httpd_resp_send_err(r, HTTPD_400_BAD_REQUEST, "Missing field"); return ESP_FAIL; }
    p->saved = true;
    httpd_resp_set_type(r, "text/html");
    httpd_resp_sendstr(r, "<!doctype html><meta charset=utf-8><p>Saved. The device connects now; this network closes.</p>");
    xSemaphoreGive(p->done);
    return ESP_OK;
}

/* The AP password: Kconfig override or a random one kept in NVS from the first start (serial log / display). */
static void ap_password(char *out, size_t cap)
{
    if (CONFIG_HOMEAPPS_SETUP_AP_PASSWORD[0] && strlen(CONFIG_HOMEAPPS_SETUP_AP_PASSWORD) >= 8) {
        strlcpy(out, CONFIG_HOMEAPPS_SETUP_AP_PASSWORD, cap);
        return;
    }
    nvs_handle_t h;
    size_t n = cap;
    if (nvs_open("homeapps_ap", NVS_READWRITE, &h) != ESP_OK) { strlcpy(out, "changeme-ap", cap); return; }
    if (nvs_get_str(h, "pw", out, &n) != ESP_OK) {
        static const char alphabet[] = "abcdefghjkmnpqrstuvwxyz23456789"; /* no look-alikes */
        for (int i = 0; i < 12; i++) out[i] = alphabet[esp_random() % (sizeof(alphabet) - 1)];
        out[12] = 0;
        nvs_set_str(h, "pw", out);
        nvs_commit(h);
    }
    nvs_close(h);
}

esp_err_t homeapps_setup_run(const char *fingerprint_hint, void (*on_password)(const char *, const char *, void *), void *user)
{
    (void)fingerprint_hint;
    static portal_t portal;
    memset(&portal, 0, sizeof(portal));
    portal.done = xSemaphoreCreateBinary();

    char suffix[8], ssid[32], pw[65];
    snprintf(suffix, sizeof(suffix), "%04X", (unsigned)(esp_random() & 0xFFFF));
    snprintf(ssid, sizeof(ssid), "HomeApps-%s", suffix);
    ap_password(pw, sizeof(pw));

    esp_netif_create_default_wifi_ap();
    wifi_config_t ap = { .ap = { .max_connection = 2, .authmode = WIFI_AUTH_WPA2_PSK, .channel = 1 } };
    strlcpy((char *)ap.ap.ssid, ssid, sizeof(ap.ap.ssid));
    ap.ap.ssid_len = (uint8_t)strlen(ssid);
    strlcpy((char *)ap.ap.password, pw, sizeof(ap.ap.password));
    esp_wifi_set_mode(WIFI_MODE_AP);
    esp_wifi_set_config(WIFI_IF_AP, &ap);
    esp_wifi_start();
    ESP_LOGW(TAG, "Setup network: SSID '%s', password '%s', open http://192.168.4.1", ssid, pw);
    if (on_password) on_password(ssid, pw, user);

    httpd_handle_t srv = NULL;
    httpd_config_t cfg = HTTPD_DEFAULT_CONFIG();
    esp_err_t result = ESP_ERR_TIMEOUT;
    if (httpd_start(&srv, &cfg) == ESP_OK) {
        httpd_uri_t get = { .uri = "/", .method = HTTP_GET, .handler = get_page };
        httpd_uri_t save = { .uri = "/save", .method = HTTP_POST, .handler = post_save, .user_ctx = &portal };
        httpd_register_uri_handler(srv, &get);
        httpd_register_uri_handler(srv, &save);
        if (xSemaphoreTake(portal.done, pdMS_TO_TICKS(CONFIG_HOMEAPPS_SETUP_TIMEOUT_S * 1000)) == pdTRUE && portal.saved) {
            vTaskDelay(pdMS_TO_TICKS(1500)); /* let the browser get the answer */
            homeapps_store_save(&portal.cfg);
            result = ESP_OK;
        }
        httpd_stop(srv);
    }
    esp_wifi_stop();
    vSemaphoreDelete(portal.done);
    return result;
}
