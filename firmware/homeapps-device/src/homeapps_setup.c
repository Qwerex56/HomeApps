/* Setup portal: WPA2 access point with a random password (first start), one form, closes on success or timeout. */
#include <ctype.h>
#include <string.h>
#include <stdio.h>
#include "esp_log.h"
#include "esp_wifi.h"
#include "esp_netif.h"
#include "esp_random.h"
#include "bootloader_random.h"
#include "esp_http_server.h"
#include "esp_timer.h"
#include "nvs.h"
#include "freertos/FreeRTOS.h"
#include "freertos/semphr.h"
#include "homeapps_internal.h"

static const char *TAG = "homeapps_setup";

static const char PAGE_HEAD[] =
    "<!doctype html><html><head><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'>"
    "<title>HomeApps</title><style>body{font-family:sans-serif;max-width:26rem;margin:2rem auto;padding:0 1rem}"
    "input,button{width:100%;padding:.6rem;margin:.3rem 0 1rem;box-sizing:border-box}</style></head><body>"
    "<h1>HomeApps</h1><form method=post action=/save>";

typedef struct { SemaphoreHandle_t done; homeapps_store_t cfg; bool saved; } portal_t;

/* Sends an HTML-escaped value (it lands in an attribute). */
static void send_escaped(httpd_req_t *r, const char *v)
{
    for (; *v; v++) {
        const char *rep = *v == '&' ? "&amp;" : *v == '<' ? "&lt;" : *v == '>' ? "&gt;" : *v == '\'' ? "&#39;" : *v == '"' ? "&quot;" : NULL;
        if (rep) httpd_resp_sendstr_chunk(r, rep);
        else httpd_resp_send_chunk(r, v, 1);
    }
}

static void send_input(httpd_req_t *r, const char *label, const char *name, const char *value, const char *extra)
{
    httpd_resp_sendstr_chunk(r, "<label>"); httpd_resp_sendstr_chunk(r, label);
    httpd_resp_sendstr_chunk(r, "</label><input name="); httpd_resp_sendstr_chunk(r, name);
    httpd_resp_sendstr_chunk(r, " value='"); send_escaped(r, value);
    httpd_resp_sendstr_chunk(r, "' "); httpd_resp_sendstr_chunk(r, extra); httpd_resp_sendstr_chunk(r, ">");
}

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
    const portal_t *p = r->user_ctx;
    const homeapps_store_t *c = &p->cfg;   /* passwords are never sent back */
    httpd_resp_set_type(r, "text/html");
    httpd_resp_sendstr_chunk(r, PAGE_HEAD);
    send_input(r, "Wi-Fi network (2.4 GHz only)", "ssid", c->ssid, "required maxlength=32");
    send_input(r, c->wifi_password[0] ? "Wi-Fi password (empty keeps the saved one)" : "Wi-Fi password", "pass", "", "type=password maxlength=64");
    send_input(r, "HomeApps address", "url", c->base_url, "placeholder='https://home.example.com' required maxlength=127");
    send_input(r, c->secret[0] ? "New pairing code (empty keeps this device paired)" : "Pairing code", "code", "",
               c->secret[0] ? "maxlength=8 autocapitalize=characters" : "required maxlength=8 autocapitalize=characters");
    send_input(r, "Server certificate fingerprint (only for trust on first use; empty = pin on first connection)", "fp", c->fingerprint, "maxlength=95");
    httpd_resp_sendstr_chunk(r, "<button type=submit>Save</button></form></body></html>");
    return httpd_resp_sendstr_chunk(r, NULL);
}

static esp_err_t post_save(httpd_req_t *r)
{
    portal_t *p = r->user_ctx;
    char body[512];
    if (r->content_len >= sizeof(body)) { httpd_resp_send_err(r, HTTPD_400_BAD_REQUEST, "Too large"); return ESP_FAIL; }
    int got = 0;
    int64_t deadline = esp_timer_get_time() + 5000000;   /* 5 s overall */
    while (got < (int)r->content_len) {   /* the body can arrive in several pieces */
        if (esp_timer_get_time() > deadline) return ESP_FAIL;
        int n = httpd_req_recv(r, body + got, r->content_len - got);
        if (n == HTTPD_SOCK_ERR_TIMEOUT) continue;
        if (n <= 0) return ESP_FAIL;
        got += n;
    }
    body[got] = 0;
    homeapps_store_t draft = p->cfg;   /* a rejected form must not change the saved values */
    homeapps_store_t *c = &draft;   /* starts as the saved store: secret, device id and pin survive unless replaced */
    char pass[65], code[16];
    field(body, "ssid", c->ssid, sizeof(c->ssid));
    field(body, "pass", pass, sizeof(pass));
    if (pass[0]) strlcpy(c->wifi_password, pass, sizeof(c->wifi_password));     /* empty keeps the saved password */
    field(body, "url", c->base_url, sizeof(c->base_url));
    field(body, "code", code, sizeof(code));
    field(body, "fp", c->fingerprint, sizeof(c->fingerprint));
    for (char *q = code; *q; q++) *q = (char)toupper((unsigned char)*q);
    size_t ul = strlen(c->base_url);
    while (ul && c->base_url[ul - 1] == '/') c->base_url[--ul] = 0;
    if (code[0]) {   /* a new code means a new pairing: forget the old identity */
        strlcpy(c->code, code, sizeof(c->code));
        c->device_id[0] = 0;
        c->secret[0] = 0;
    }
    if (!c->ssid[0] || !c->base_url[0] || (!c->code[0] && !c->secret[0])) { httpd_resp_send_err(r, HTTPD_400_BAD_REQUEST, "Missing field"); return ESP_FAIL; }
    p->cfg = draft;
    p->saved = true;
    httpd_resp_set_type(r, "text/html");
    httpd_resp_sendstr(r, "<!doctype html><meta charset=utf-8><p>Saved. The device connects now; this network closes.</p>");
    xSemaphoreGive(p->done);
    return ESP_OK;
}

static void random_pw(char *out)
{
    static const char alphabet[] = "abcdefghjkmnpqrstuvwxyz23456789"; /* no look-alikes */
    for (int i = 0; i < 12; i++) out[i] = alphabet[esp_random() % (sizeof(alphabet) - 1)];
    out[12] = 0;
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
    if (nvs_open("homeapps_ap", NVS_READWRITE, &h) != ESP_OK) { random_pw(out); return; }   /* NVS unavailable: random for this boot, never a constant */
    if (nvs_get_str(h, "pw", out, &n) != ESP_OK) {
        random_pw(out);
        nvs_set_str(h, "pw", out);
        nvs_commit(h);
    }
    nvs_close(h);
}

esp_err_t homeapps_setup_run(const homeapps_store_t *current, void (*on_password)(const char *, const char *, void *), void *user)
{
    static portal_t portal;
    memset(&portal, 0, sizeof(portal));
    portal.cfg = *current;
    portal.done = xSemaphoreCreateBinary();

    char suffix[8], ssid[32], pw[65];
    bootloader_random_enable();   /* esp_random() is only random with an entropy source: RF is still off here */
    snprintf(suffix, sizeof(suffix), "%04X", (unsigned)(esp_random() & 0xFFFF));
    snprintf(ssid, sizeof(ssid), "HomeApps-%s", suffix);
    ap_password(pw, sizeof(pw));
    bootloader_random_disable();

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
        httpd_uri_t get = { .uri = "/", .method = HTTP_GET, .handler = get_page, .user_ctx = &portal };
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
