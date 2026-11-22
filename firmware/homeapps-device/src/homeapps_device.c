#include <ctype.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <time.h>
#include "esp_log.h"
#include "esp_wifi.h"
#include "esp_event.h"
#include "esp_netif.h"
#include "esp_system.h"
#include "esp_timer.h"
#include "esp_sntp.h"
#include "esp_http_client.h"
#include "esp_websocket_client.h"
#include "esp_crt_bundle.h"
#include "esp_tls.h"
#include "mbedtls/sha256.h"
#include "mbedtls/ssl.h"
#include "mbedtls/x509_crt.h"
#include "nvs_flash.h"
#include "nvs.h"
#include "driver/gpio.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"
#include "freertos/semphr.h"
#include "freertos/queue.h"
#include "homeapps_internal.h"

static const char *TAG = "homeapps";
extern const char pinned_ca_pem_start[] asm("_binary_pinned_ca_pem_start");

#define GOT_IP_BIT BIT0
#define READY_BIT BIT1
#define LINK_DOWN_BIT BIT2
#define MAX_QUEUE HOMEAPPS_MAX_REPORT_FRAMES
#define UNPAIRED_AFTER_BAD_LOGINS 3

static struct {
    homeapps_config_t cfg;
    homeapps_store_t store;
    EventGroupHandle_t events;
    SemaphoreHandle_t lock;
    char token[2048];
    SemaphoreHandle_t ws_lock;
    SemaphoreHandle_t rate_lock;
    QueueHandle_t inbox;   /* messages from the ws callback, handled in link_task (no sends from inside the callback) */
    int64_t token_expiry_us;
    int bad_logins;
    esp_websocket_client_handle_t ws;
    char *queue[MAX_QUEUE];
    int queued;
    int64_t window_start_us;
    int frames_in_window;
    int battery;  /* last reported, -1 = unknown */
    float voltage;
    bool started;
} S = { .battery = -1 };

/* ---------- small helpers ---------- */

static bool clock_synced(void) { return time(NULL) > 1700000000; }

static int backoff_ms(int attempt, int retry_after_s)
{
    int cap_s = (S.cfg.descriptor && S.cfg.descriptor->sleepy) ? 30 : 300;   /* a server cannot park the device for hours */
    if (retry_after_s > 0) return (retry_after_s > cap_s ? cap_s : retry_after_s) * 1000;
    int ms = 2000 << (attempt > 6 ? 6 : attempt);
    return ms > 120000 ? 120000 : ms;
}

static void to_setup_and_restart(const char *why)
{
    ESP_LOGW(TAG, "Back to setup: %s", why);
    homeapps_store_erase_credentials();
    esp_restart();
}

/* ---------- HTTP (pairing, token, report) ---------- */

typedef struct { char *body; size_t len; size_t cap; int retry_after; } http_out_t;

static esp_err_t http_event(esp_http_client_event_t *e)
{
    http_out_t *o = e->user_data;
    if (e->event_id == HTTP_EVENT_ON_HEADER && !strcasecmp(e->header_key, "Retry-After")) o->retry_after = atoi(e->header_value);
    if (e->event_id == HTTP_EVENT_ON_DATA && o->len + e->data_len < o->cap) {
        memcpy(o->body + o->len, e->data, e->data_len);
        o->len += e->data_len;
        o->body[o->len] = 0;
    }
    return ESP_OK;
}

#ifdef CONFIG_HOMEAPPS_TLS_TOFU
/* Trust on first use, bound to the real connection: mbedtls calls this for every certificate of the handshake. We pin the
 * SHA-256 of the leaf (depth 0) on the first connection and refuse any other one afterwards. */
static int tofu_verify(void *ctx, mbedtls_x509_crt *crt, int depth, uint32_t *flags)
{
    (void)ctx;
    if (depth != 0) { *flags = 0; return 0; }
    unsigned char h[32];
    mbedtls_sha256(crt->raw.p, crt->raw.len, h, 0);
    char hex[72];
    for (int i = 0; i < 32; i++) sprintf(hex + i * 2, "%02X", h[i]);
    if (!S.store.fingerprint[0]) {
        ESP_LOGW(TAG, "Pinning server certificate SHA-256 %s", hex);
        strlcpy(S.store.fingerprint, hex, sizeof(S.store.fingerprint));
        homeapps_store_save(&S.store);
    } else {
        char want[72]; size_t j = 0;  /* the form accepts colons and lowercase */
        for (const char *c = S.store.fingerprint; *c && j < 64; c++) if (*c != ':') want[j++] = (char)toupper((unsigned char)*c);
        want[j] = 0;
        if (strcmp(want, hex)) { ESP_LOGE(TAG, "Server certificate changed (%s): refusing", hex); return MBEDTLS_ERR_X509_CERT_VERIFY_FAILED; }
    }
    *flags = 0;   /* the pin replaces chain and name checks */
    return 0;
}

/* Used as crt_bundle_attach by esp_http_client and esp_websocket_client. mbedtls refuses VERIFY_REQUIRED without any CA
 * chain, so an empty (non-NULL) one is set; the callback above makes the decision. */
static esp_err_t tofu_attach(void *conf)
{
    static mbedtls_x509_crt empty;
    mbedtls_x509_crt_init(&empty);
    mbedtls_ssl_config *c = conf;
    mbedtls_ssl_conf_authmode(c, MBEDTLS_SSL_VERIFY_REQUIRED);
    mbedtls_ssl_conf_ca_chain(c, &empty, NULL);
    mbedtls_ssl_conf_verify(c, tofu_verify, NULL);
    return ESP_OK;
}
#endif

/* POST json; returns the HTTP status or -1. */
static int http_json(const char *path, const char *bearer, const char *body, http_out_t *out)
{
    char url[256];
    snprintf(url, sizeof(url), "%s%s", S.store.base_url, path);
    bool https = !strncmp(url, "https://", 8);
    if (!https && !HOMEAPPS_INSECURE) { ESP_LOGE(TAG, "Plain http is off (Kconfig HOMEAPPS_INSECURE_HTTP)"); return -1; }
    esp_http_client_config_t c = { .url = url, .method = HTTP_METHOD_POST, .event_handler = http_event, .user_data = out, .timeout_ms = 15000 };
#if defined(CONFIG_HOMEAPPS_TLS_BUNDLE)
    c.crt_bundle_attach = esp_crt_bundle_attach;
#elif defined(CONFIG_HOMEAPPS_TLS_PINNED_CA)
    c.cert_pem = pinned_ca_pem_start;
#else
    c.skip_cert_common_name_check = true;
    c.crt_bundle_attach = tofu_attach;
#endif
    esp_http_client_handle_t h = esp_http_client_init(&c);
    if (!h) return -1;
    esp_http_client_set_header(h, "Content-Type", "application/json");
    if (bearer) {
        char auth[2100];
        snprintf(auth, sizeof(auth), "Bearer %s", bearer);
        esp_http_client_set_header(h, "Authorization", auth);
    }
    esp_http_client_set_post_field(h, body, (int)strlen(body));
    int status = esp_http_client_perform(h) == ESP_OK ? esp_http_client_get_status_code(h) : -1;
    esp_http_client_cleanup(h);
    return status;
}

static void out_init(http_out_t *o, size_t cap)
{
    o->body = calloc(1, cap); o->len = 0; o->cap = cap; o->retry_after = 0;
}

/* Pairing: sends the code and the descriptor, keeps the secret. Retries on 429/5xx; a bad code goes back to setup. */
static esp_err_t pair(int max_attempts)
{
    char *desc = homeapps_descriptor_to_json(S.cfg.descriptor);
    cJSON *req = cJSON_CreateObject();
    cJSON_AddStringToObject(req, "code", S.store.code);
    cJSON_AddItemToObject(req, "descriptor", cJSON_Parse(desc));
    free(desc);
    char *body = cJSON_PrintUnformatted(req);
    cJSON_Delete(req);
    for (int attempt = 0; max_attempts == 0 || attempt < max_attempts; attempt++) {
        http_out_t out; out_init(&out, 2048);
        int status = http_json("/devices/pair", NULL, body, &out);
        if (status == 200) {
            cJSON *r = cJSON_Parse(out.body);
            const cJSON *id = cJSON_GetObjectItem(r, "deviceId"), *sec = cJSON_GetObjectItem(r, "secret");
            esp_err_t e = ESP_FAIL;
            if (cJSON_IsString(id) && cJSON_IsString(sec)) {
                strlcpy(S.store.device_id, id->valuestring, sizeof(S.store.device_id));
                strlcpy(S.store.secret, sec->valuestring, sizeof(S.store.secret));
                S.store.code[0] = 0;
                e = homeapps_store_save(&S.store);
            }
            cJSON_Delete(r); free(out.body); free(body);
            return e;
        }
        int wait = backoff_ms(attempt, out.retry_after);
        free(out.body);
        if (status >= 400 && status < 500 && status != 429) {
            free(body);
            ESP_LOGW(TAG, "Pairing refused (%d): code used, expired or descriptor invalid", status);
            S.store.code[0] = 0;               /* Wi-Fi and address stay; setup opens for a new code */
            homeapps_store_save(&S.store);
            esp_restart();
        }
        ESP_LOGW(TAG, "Pairing failed (%d), retry in %d ms", status, wait);
        vTaskDelay(pdMS_TO_TICKS(wait));
    }
    free(body);
    return ESP_ERR_TIMEOUT;
}

/* A fresh token for every connection and every /report. Returns ESP_OK with S.token filled. */
static esp_err_t fetch_token(int max_attempts)
{
    char body[200];
    snprintf(body, sizeof(body), "{\"deviceId\":\"%s\",\"secret\":\"%s\"}", S.store.device_id, S.store.secret);
    for (int attempt = 0; max_attempts == 0 || attempt < max_attempts; attempt++) {
        http_out_t out; out_init(&out, 2048);
        int status = http_json("/auth/devices/token", NULL, body, &out);
        if (status == 200) {
            cJSON *r = cJSON_Parse(out.body);
            const cJSON *t = cJSON_GetObjectItem(r, "accessToken");
            esp_err_t e = ESP_FAIL;
            if (cJSON_IsString(t) && strlen(t->valuestring) < sizeof(S.token)) { strcpy(S.token, t->valuestring); e = ESP_OK; S.bad_logins = 0; }
            cJSON_Delete(r); free(out.body);
            if (e == ESP_OK) return e;
            ESP_LOGW(TAG, "Token answer malformed or too long");   /* back off below instead of a hot loop */
            vTaskDelay(pdMS_TO_TICKS(backoff_ms(attempt, 0)));
            continue;
        }
        if (status == 401) {
            if (out.body && strstr(out.body, "device_unpaired")) { free(out.body); to_setup_and_restart("devices.device_unpaired"); }
            if (++S.bad_logins >= UNPAIRED_AFTER_BAD_LOGINS) { free(out.body); to_setup_and_restart("3 invalid_credentials in a row"); }
        }
        int wait = backoff_ms(attempt, out.retry_after);
        free(out.body);
        ESP_LOGW(TAG, "Token request failed (%d), retry in %d ms", status, wait);
        vTaskDelay(pdMS_TO_TICKS(wait));
    }
    return ESP_ERR_TIMEOUT;
}

/* ---------- frames ---------- */

static bool rate_ok(void)
{
    int64_t now = esp_timer_get_time();
    xSemaphoreTake(S.rate_lock, portMAX_DELAY);
    if (now - S.window_start_us > 60LL * 1000000) { S.window_start_us = now; S.frames_in_window = 0; }
    bool ok = S.frames_in_window < HOMEAPPS_MAX_FRAMES_PER_MINUTE;
    if (ok) S.frames_in_window++;
    xSemaphoreGive(S.rate_lock);
    return ok;
}

/* All sends go through ws_lock: link_task destroys the client under the same lock, so a send never touches a dead handle. */
static int ws_send(const char *text)
{
    int n = -1;
    xSemaphoreTake(S.ws_lock, portMAX_DELAY);
    if (S.ws) n = esp_websocket_client_send_text(S.ws, text, (int)strlen(text), pdMS_TO_TICKS(5000));
    xSemaphoreGive(S.ws_lock);
    return n;
}

static bool link_up(void) { return !S.cfg.descriptor->sleepy && (xEventGroupGetBits(S.events) & READY_BIT) && S.ws; }

static void enqueue(char *frame)
{
    xSemaphoreTake(S.lock, portMAX_DELAY);
    if (S.queued == MAX_QUEUE) { free(S.queue[0]); memmove(S.queue, S.queue + 1, sizeof(S.queue[0]) * (MAX_QUEUE - 1)); S.queued--; }
    S.queue[S.queued++] = frame;
    xSemaphoreGive(S.lock);
}

static esp_err_t send_frame(cJSON *frame)
{
    char *s = cJSON_PrintUnformatted(frame);
    cJSON_Delete(frame);
    if (!s) return ESP_ERR_NO_MEM;
    if (link_up()) {
        if (!rate_ok()) { free(s); return ESP_ERR_NOT_ALLOWED; } /* above 120/min the server would drop it */
        int n = ws_send(s);
        free(s);
        return n < 0 ? ESP_FAIL : ESP_OK;
    }
    enqueue(s);
    return ESP_OK;
}

esp_err_t homeapps_send_state(const char *values_json)
{
    cJSON *v = cJSON_Parse(values_json);
    if (!v) return ESP_ERR_INVALID_ARG;
    cJSON *f = cJSON_CreateObject();
    cJSON_AddStringToObject(f, "type", "state");
    cJSON_AddItemToObject(f, "values", v);
    return send_frame(f);
}

esp_err_t homeapps_send_event(const char *name, const char *data_json)
{
    cJSON *f = cJSON_CreateObject();
    cJSON_AddStringToObject(f, "type", "event");
    cJSON_AddStringToObject(f, "name", name);
    if (clock_synced()) { /* without SNTP no "at": the server uses its receive time */
        char at[32]; time_t t = time(NULL); struct tm tm; gmtime_r(&t, &tm);
        strftime(at, sizeof(at), "%Y-%m-%dT%H:%M:%SZ", &tm);
        cJSON_AddStringToObject(f, "at", at);
    }
    if (data_json) { cJSON *d = cJSON_Parse(data_json); if (d) cJSON_AddItemToObject(f, "data", d); }
    return send_frame(f);
}

esp_err_t homeapps_send_status(int battery_percent, float voltage, const char *error)
{
    cJSON *f = cJSON_CreateObject();
    cJSON_AddStringToObject(f, "type", "status");
    if (battery_percent >= 0) { cJSON_AddNumberToObject(f, "battery", battery_percent); S.battery = battery_percent; }
    if (voltage > 0) { cJSON_AddNumberToObject(f, "voltage", voltage); S.voltage = voltage; }
    wifi_ap_record_t ap;
    if (esp_wifi_sta_get_ap_info(&ap) == ESP_OK) cJSON_AddNumberToObject(f, "rssi", ap.rssi);
    cJSON_AddNumberToObject(f, "uptime", (double)(esp_timer_get_time() / 1000000));
    cJSON_AddStringToObject(f, "firmware", S.cfg.descriptor->version);
    if (error && error[0]) cJSON_AddStringToObject(f, "error", error);
    return send_frame(f);
}

esp_err_t homeapps_flush(void)
{
    xSemaphoreTake(S.lock, portMAX_DELAY);
    int n = S.queued;
    if (n == 0) { xSemaphoreGive(S.lock); return ESP_OK; }
    size_t total = 2;
    for (int i = 0; i < n; i++) total += strlen(S.queue[i]) + 1;
    if (total > HOMEAPPS_MAX_REPORT_BYTES) { /* keep the newest that fit */
        while (n > 1 && total > HOMEAPPS_MAX_REPORT_BYTES) { total -= strlen(S.queue[0]) + 1; free(S.queue[0]); memmove(S.queue, S.queue + 1, sizeof(S.queue[0]) * (--n)); }
        S.queued = n;
    }
    char *body = malloc(total + 1);
    char *p = body; *p++ = '[';
    for (int i = 0; i < n; i++) { if (i) *p++ = ','; size_t l = strlen(S.queue[i]); memcpy(p, S.queue[i], l); p += l; }
    *p++ = ']'; *p = 0;
    xSemaphoreGive(S.lock);

    esp_err_t result = ESP_FAIL;
    for (int attempt = 0; attempt < 4; attempt++) {
        if (fetch_token(3) != ESP_OK) break;
        http_out_t out; out_init(&out, 4096);
        int status = http_json("/devices/report", S.token, body, &out);
        if (status == 200) {
            xSemaphoreTake(S.lock, portMAX_DELAY);
            for (int i = 0; i < n; i++) free(S.queue[i]);
            memmove(S.queue, S.queue + n, sizeof(S.queue[0]) * (S.queued - n));
            S.queued -= n;
            xSemaphoreGive(S.lock);
            result = ESP_OK; free(out.body);
            break;
        }
        int wait = backoff_ms(attempt, out.retry_after); /* 429 / 5xx: wait Retry-After */
        free(out.body);
        if (status == 401) continue;
        if (status >= 400 && status < 500 && status != 429) { ESP_LOGE(TAG, "Report refused (%d), dropping frames", status);
            xSemaphoreTake(S.lock, portMAX_DELAY);
            for (int i = 0; i < S.queued; i++) free(S.queue[i]);
            S.queued = 0;
            xSemaphoreGive(S.lock);
            break;
        }
        vTaskDelay(pdMS_TO_TICKS(wait));
    }
    free(body);
    return result;
}

/* ---------- link (WebSocket) ---------- */

/* Frames queued while offline go out under the 120/min limit; the rest waits for the next tick. A failed send puts the
 * frame back. The lock is held only to take or return a frame, never across a send. link_task only. */
static void drain_queue(void)
{
    for (;;) {
        xSemaphoreTake(S.lock, portMAX_DELAY);
        char *frame = NULL;
        if (S.queued > 0) { frame = S.queue[0]; memmove(S.queue, S.queue + 1, sizeof(S.queue[0]) * (--S.queued)); }
        xSemaphoreGive(S.lock);
        if (!frame) return;
        bool sent = rate_ok() && ws_send(frame) >= 0;
        if (sent) { free(frame); continue; }
        xSemaphoreTake(S.lock, portMAX_DELAY);      /* back to the front */
        if (S.queued < MAX_QUEUE) { memmove(S.queue + 1, S.queue, sizeof(S.queue[0]) * S.queued); S.queue[0] = frame; S.queued++; }
        else free(frame);
        xSemaphoreGive(S.lock);
        return;
    }
}

static char *rx; static size_t rx_len;

/* Runs in link_task. Returns true for "ready". */
static bool process_message(const char *text)
{
    bool ready = false;
    cJSON *m = cJSON_Parse(text);
    if (!m) return false;
    const char *type = cJSON_GetStringValue(cJSON_GetObjectItem(m, "type"));
    if (type && !strcmp(type, "@connected")) {   /* first frame within 10 s */
        char *desc = homeapps_descriptor_to_json(S.cfg.descriptor);
        size_t cap = desc ? strlen(desc) + 256 : 0;
        char *hello = desc ? malloc(cap) : NULL;
        if (!hello) { free(desc); cJSON_Delete(m); return false; }
        snprintf(hello, cap, "{\"type\":\"hello\",\"device\":\"%s\",\"kind\":\"satellite\",\"descriptor\":%s}", S.cfg.descriptor->model, desc);
        ws_send(hello);
        free(desc); free(hello);
    } else if (type && !strcmp(type, "ready")) {
        ready = true;
        xEventGroupSetBits(S.events, READY_BIT);
        homeapps_send_status(-1, 0, NULL);
        drain_queue();
    } else if (type && !strcmp(type, "set")) {
        const cJSON *id = cJSON_GetObjectItem(m, "id"), *name = cJSON_GetObjectItem(m, "name"), *value = cJSON_GetObjectItem(m, "value");
        char *vjson = cJSON_PrintUnformatted(value);
        esp_err_t e = S.cfg.on_set ? S.cfg.on_set(cJSON_GetStringValue((cJSON *)name), vjson, S.cfg.user) : ESP_ERR_NOT_SUPPORTED;
        free(vjson);
        cJSON *r = cJSON_CreateObject();
        cJSON_AddStringToObject(r, "type", "set_result");
        cJSON_AddItemToObject(r, "id", cJSON_Duplicate(id, 1));
        cJSON_AddBoolToObject(r, "ok", e == ESP_OK);
        if (e != ESP_OK) cJSON_AddStringToObject(r, "text", esp_err_to_name(e));
        char *s = cJSON_PrintUnformatted(r);
        ws_send(s);
        free(s); cJSON_Delete(r);
    } else if (type && !strcmp(type, "error")) {
        ESP_LOGW(TAG, "Server: %s", text);
    }
    cJSON_Delete(m);
    return ready;
}

static void inbox_push(const char *text)
{
    char *copy = strdup(text);
    /* A short wait instead of dropping "ready" or "set" when link_task is busy (a set handler can take a moment). */
    if (copy && xQueueSend(S.inbox, &copy, pdMS_TO_TICKS(100)) == pdTRUE) return;
    ESP_LOGW(TAG, "Inbox full or out of memory: message dropped");
    free(copy);
}

static void ws_event(void *arg, esp_event_base_t base, int32_t id, void *data)
{
    esp_websocket_event_data_t *e = data;
    if (id == WEBSOCKET_EVENT_CONNECTED) {
        inbox_push("{\"type\":\"@connected\"}");   /* no sends from inside the callback */
    } else if (id == WEBSOCKET_EVENT_DATA && (e->op_code == 0x1 || e->op_code == 0x0)) {
        if (e->payload_offset == 0) { free(rx); rx = malloc(e->payload_len + 1); rx_len = 0; }
        if (rx && (size_t)e->payload_offset + (size_t)e->data_len > (size_t)e->payload_len) { free(rx); rx = NULL; }   /* overrun: drop */
        if (rx) {
            memcpy(rx + e->payload_offset, e->data_ptr, e->data_len);
            rx_len = e->payload_offset + e->data_len;
            if (rx_len >= (size_t)e->payload_len) { rx[rx_len] = 0; inbox_push(rx); free(rx); rx = NULL; }
        }
    } else if (id == WEBSOCKET_EVENT_DISCONNECTED || id == WEBSOCKET_EVENT_ERROR || id == WEBSOCKET_EVENT_CLOSED ||
               (id == WEBSOCKET_EVENT_DATA && e->op_code == 0x08)) {
        /* A clean server close (e.g. a refused hello) arrives as CLOSED or a close frame, not as DISCONNECTED. */
        free(rx); rx = NULL;
        xEventGroupClearBits(S.events, READY_BIT);
        xEventGroupSetBits(S.events, LINK_DOWN_BIT);
    }
}

static void link_task(void *arg)
{
    int attempt = 0;
    int heartbeat = S.cfg.descriptor->heartbeat_s ? S.cfg.descriptor->heartbeat_s : 60;
    for (;;) {
        xEventGroupWaitBits(S.events, GOT_IP_BIT, pdFALSE, pdTRUE, portMAX_DELAY);
        if (fetch_token(0) != ESP_OK) continue;           /* a new token for every connection; the link task never gives up */
        char url[200], headers[2100];
        {
            const char *rest = strstr(S.store.base_url, "://");
            rest = rest ? rest + 3 : S.store.base_url;
            snprintf(url, sizeof(url), "%s://%s/devices/link", !strncmp(S.store.base_url, "https", 5) ? "wss" : "ws", rest);
        }
        snprintf(headers, sizeof(headers), "Authorization: Bearer %s\r\n", S.token);
        esp_websocket_client_config_t c = { .uri = url, .headers = headers, .disable_auto_reconnect = true, .network_timeout_ms = 15000 };
#if defined(CONFIG_HOMEAPPS_TLS_BUNDLE)
        c.crt_bundle_attach = esp_crt_bundle_attach;
#elif defined(CONFIG_HOMEAPPS_TLS_PINNED_CA)
        c.cert_pem = pinned_ca_pem_start;
#else
        c.skip_cert_common_name_check = true;
        c.crt_bundle_attach = tofu_attach;
#endif
        xEventGroupClearBits(S.events, LINK_DOWN_BIT);
        if (strncmp(url, "wss", 3) && !HOMEAPPS_INSECURE) {
            ESP_LOGE(TAG, "Plain ws is off (Kconfig HOMEAPPS_INSECURE_HTTP)");
            vTaskDelay(pdMS_TO_TICKS(backoff_ms(attempt++, 0)));
            continue;
        }
        esp_websocket_client_handle_t client = esp_websocket_client_init(&c);
        if (!client) { ESP_LOGE(TAG, "websocket init failed"); vTaskDelay(pdMS_TO_TICKS(backoff_ms(attempt++, 0))); continue; }
        esp_websocket_register_events(client, WEBSOCKET_EVENT_ANY, ws_event, NULL);
        xSemaphoreTake(S.ws_lock, portMAX_DELAY);
        S.ws = client;
        xSemaphoreGive(S.ws_lock);
        char *stale;
        while (xQueueReceive(S.inbox, &stale, 0) == pdTRUE) free(stale);
        bool was_ready = false;
        if (esp_websocket_client_start(client) != ESP_OK) {
            ESP_LOGE(TAG, "websocket start failed");
            xEventGroupSetBits(S.events, LINK_DOWN_BIT);
        }
        int64_t last_status = esp_timer_get_time();
        while (!(xEventGroupGetBits(S.events) & LINK_DOWN_BIT)) {
            char *msg;
            if (xQueueReceive(S.inbox, &msg, pdMS_TO_TICKS(500)) == pdTRUE) { was_ready |= process_message(msg); free(msg); continue; }
            if (xEventGroupGetBits(S.events) & READY_BIT) drain_queue();
            if ((xEventGroupGetBits(S.events) & READY_BIT) && esp_timer_get_time() - last_status > heartbeat * 1000000LL) {
                homeapps_send_status(S.battery, S.voltage, NULL);
                last_status = esp_timer_get_time();
            }
        }
        attempt = was_ready ? 0 : attempt + 1;
        xSemaphoreTake(S.ws_lock, portMAX_DELAY);
        S.ws = NULL;
        xSemaphoreGive(S.ws_lock);
        esp_websocket_client_destroy(client);
        vTaskDelay(pdMS_TO_TICKS(backoff_ms(attempt, 0)));
    }
}

/* ---------- start, Wi-Fi, button ---------- */

static void net_event(void *arg, esp_event_base_t base, int32_t id, void *data)
{
    if (base == WIFI_EVENT && id == WIFI_EVENT_STA_START) esp_wifi_connect();
    else if (base == WIFI_EVENT && id == WIFI_EVENT_STA_DISCONNECTED) { xEventGroupClearBits(S.events, GOT_IP_BIT); esp_wifi_connect(); }
    else if (base == IP_EVENT && id == IP_EVENT_STA_GOT_IP) xEventGroupSetBits(S.events, GOT_IP_BIT);
}

static void set_force_setup(bool on)
{
    nvs_handle_t h;
    if (nvs_open("homeapps_ap", NVS_READWRITE, &h) == ESP_OK) { nvs_set_u8(h, "force", on); nvs_commit(h); nvs_close(h); }
}

static bool take_force_setup(void)
{
    nvs_handle_t h; uint8_t v = 0;
    if (nvs_open("homeapps_ap", NVS_READWRITE, &h) != ESP_OK) return false;
    nvs_get_u8(h, "force", &v);
    if (v) { nvs_set_u8(h, "force", 0); nvs_commit(h); }
    nvs_close(h);
    return v;
}

void homeapps_open_setup(void) { set_force_setup(true); esp_restart(); }

void homeapps_factory_reset(void)
{
    homeapps_store_erase_all();   /* secret and Wi-Fi */
    esp_restart();
}

static void button_task(void *arg)
{
    gpio_num_t pin = (gpio_num_t)CONFIG_HOMEAPPS_RESET_BUTTON_GPIO;
    gpio_config_t io = { .pin_bit_mask = 1ULL << pin, .mode = GPIO_MODE_INPUT, .pull_up_en = GPIO_PULLUP_ENABLE };
    gpio_config(&io);
    int held_ms = 0;
    for (;;) {
        vTaskDelay(pdMS_TO_TICKS(100));
        if (gpio_get_level(pin) == 0) { held_ms += 100; if (held_ms == 5000) homeapps_factory_reset(); }
        else { if (held_ms >= 2000) homeapps_open_setup(); held_ms = 0; }
    }
}

static esp_err_t wifi_connect(void)
{
    esp_netif_create_default_wifi_sta();
    wifi_init_config_t wic = WIFI_INIT_CONFIG_DEFAULT();
    esp_wifi_init(&wic);
    esp_event_handler_register(WIFI_EVENT, ESP_EVENT_ANY_ID, net_event, NULL);
    esp_event_handler_register(IP_EVENT, IP_EVENT_STA_GOT_IP, net_event, NULL);
    wifi_config_t w = { .sta = { .threshold.authmode = WIFI_AUTH_WPA2_PSK } };
    strlcpy((char *)w.sta.ssid, S.store.ssid, sizeof(w.sta.ssid));
    strlcpy((char *)w.sta.password, S.store.wifi_password, sizeof(w.sta.password));
    esp_wifi_set_mode(WIFI_MODE_STA);
    esp_wifi_set_config(WIFI_IF_STA, &w);
    return esp_wifi_start();
}

esp_err_t homeapps_start(const homeapps_config_t *config)
{
    char err[120];
    if (homeapps_validate(config->descriptor, err, sizeof(err)) != ESP_OK) { ESP_LOGE(TAG, "Descriptor: %s", err); return ESP_ERR_INVALID_ARG; }
    S.cfg = *config;
    S.events = xEventGroupCreate();
    S.lock = xSemaphoreCreateMutex();
    S.ws_lock = xSemaphoreCreateMutex();
    S.rate_lock = xSemaphoreCreateMutex();
    S.inbox = xQueueCreate(16, sizeof(char *));
    if (!S.events || !S.lock || !S.ws_lock || !S.rate_lock || !S.inbox) return ESP_ERR_NO_MEM;
    esp_err_t e = nvs_flash_init();
    if (e == ESP_ERR_NVS_NO_FREE_PAGES || e == ESP_ERR_NVS_NEW_VERSION_FOUND) { nvs_flash_erase(); nvs_flash_init(); }
    esp_netif_init();
    esp_event_loop_create_default();
    if (CONFIG_HOMEAPPS_RESET_BUTTON_GPIO >= 0) xTaskCreate(button_task, "ha_button", 3072, NULL, 4, NULL);

    homeapps_store_load(&S.store);
    bool force = take_force_setup();
    if (force || !S.store.ssid[0] || (!S.store.secret[0] && !S.store.code[0])) {
        wifi_init_config_t wic = WIFI_INIT_CONFIG_DEFAULT();
        esp_wifi_init(&wic);
        if (homeapps_setup_run(&S.store, config->on_setup_password, config->user) != ESP_OK) { ESP_LOGW(TAG, "Setup timed out"); esp_restart(); }
        esp_wifi_deinit();
        esp_restart(); /* clean start with the saved configuration */
    }

    wifi_connect();
    if (!(xEventGroupWaitBits(S.events, GOT_IP_BIT, pdFALSE, pdTRUE, pdMS_TO_TICKS(30000)) & GOT_IP_BIT) && !S.store.secret[0])
        to_setup_and_restart("Wi-Fi did not connect (wrong password or a 5 GHz network)");
    if (config->descriptor->sleepy) {   /* a battery device gives up: the caller sleeps and tries at the next wake */
        if (!(xEventGroupGetBits(S.events) & GOT_IP_BIT)) return ESP_ERR_TIMEOUT;
    } else {
        xEventGroupWaitBits(S.events, GOT_IP_BIT, pdFALSE, pdTRUE, portMAX_DELAY);
    }

    esp_sntp_setoperatingmode(SNTP_OPMODE_POLL);
    esp_sntp_setservername(0, CONFIG_HOMEAPPS_SNTP_SERVER);
    esp_sntp_init();
    for (int i = 0; i < 100 && !clock_synced(); i++) vTaskDelay(pdMS_TO_TICKS(100)); /* events without "at" wait for SNTP */

    if (!S.store.secret[0] && pair(config->descriptor->sleepy ? 4 : 0) != ESP_OK) return ESP_ERR_TIMEOUT;
    S.started = true;
    if (!config->descriptor->sleepy) xTaskCreate(link_task, "ha_link", 8192, NULL, 5, NULL);
    return ESP_OK;
}
