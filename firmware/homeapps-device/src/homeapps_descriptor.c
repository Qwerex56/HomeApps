#include <ctype.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>
#include "homeapps_internal.h"

static bool name_ok(const char *n, bool action)
{
    if (!n || !islower((unsigned char)n[0])) return false;
    size_t len = strlen(n);
    if (action && len > 20) return false;           /* ^[a-z][a-z0-9_]{0,19}$ */
    if (len > 64) return false;
    for (size_t i = 1; i < len; i++) {
        char c = n[i];
        if (!(islower((unsigned char)c) || isdigit((unsigned char)c) || c == '_' || (!action && c == '.'))) return false;
    }
    return true;
}

static void add_items(cJSON *root, const char *key, const homeapps_item_t *items, size_t len, int kind)
{
    if (!len) return;
    cJSON *arr = cJSON_AddArrayToObject(root, key);
    for (size_t i = 0; i < len; i++) {
        const homeapps_item_t *it = &items[i];
        cJSON *o = cJSON_CreateObject();
        cJSON_AddStringToObject(o, "name", it->name);
        if (it->label) cJSON_AddStringToObject(o, "label", it->label);
        if (it->description) cJSON_AddStringToObject(o, "description", it->description);
        if (it->schema_json) {
            cJSON *schema = cJSON_Parse(it->schema_json);
            if (schema) cJSON_AddItemToObject(o, kind == 2 ? "data" : (kind == 3 ? "input" : "value"), schema);
        }
        if (kind == 0 && it->widget) cJSON_AddStringToObject(o, "widget", it->widget);
        if (kind == 1 && it->unit) cJSON_AddStringToObject(o, "unit", it->unit);
        if (kind == 2) { cJSON_AddBoolToObject(o, "notify", it->notify); if (it->location) cJSON_AddBoolToObject(o, "location", true); }
        if (kind == 3) { cJSON_AddBoolToObject(o, "readOnly", it->read_only); cJSON_AddBoolToObject(o, "confirm", it->confirm); }
        cJSON_AddItemToArray(arr, o);
    }
}

char *homeapps_descriptor_to_json(const homeapps_descriptor_t *d)
{
    cJSON *root = cJSON_CreateObject();
    cJSON_AddStringToObject(root, "format", "homeapps.device/1");
    cJSON_AddStringToObject(root, "model", d->model);
    cJSON_AddStringToObject(root, "version", d->version);
    cJSON_AddStringToObject(root, "purpose", d->purpose);
    cJSON *roles = cJSON_AddArrayToObject(root, "roles");
    for (size_t i = 0; d->roles && d->roles[i]; i++) cJSON_AddItemToArray(roles, cJSON_CreateString(d->roles[i]));
    if (d->capabilities_json) {
        cJSON *caps = cJSON_Parse(d->capabilities_json);
        if (caps) cJSON_AddItemToObject(root, "capabilities", caps);
    }
    if (d->jarvis) cJSON_AddStringToObject(root, "jarvis", d->jarvis);
    if (d->heartbeat_s) cJSON_AddNumberToObject(root, "heartbeat", d->heartbeat_s);
    if (d->sleepy) cJSON_AddBoolToObject(root, "sleepy", true);
    add_items(root, "controls", d->controls, d->controls_len, 0);
    add_items(root, "sensors", d->sensors, d->sensors_len, 1);
    add_items(root, "events", d->events, d->events_len, 2);
    add_items(root, "actions", d->actions, d->actions_len, 3);
    char *s = cJSON_PrintUnformatted(root);
    cJSON_Delete(root);
    return s;
}

#define FAIL(...) do { if (err) snprintf(err, err_len, __VA_ARGS__); return ESP_ERR_INVALID_ARG; } while (0)

static esp_err_t check_list(const homeapps_item_t *items, size_t len, const char *what, bool action, char *err, size_t err_len)
{
    if (len > HOMEAPPS_MAX_ITEMS) FAIL("%s: more than %d items", what, HOMEAPPS_MAX_ITEMS);
    for (size_t i = 0; i < len; i++) {
        if (!name_ok(items[i].name, action)) FAIL("%s: bad name '%s'", what, items[i].name ? items[i].name : "(null)");
        if (items[i].schema_json) {
            cJSON *p = cJSON_Parse(items[i].schema_json);
            if (!p) FAIL("%s '%s': schema is not valid JSON", what, items[i].name);
            cJSON_Delete(p);
            if (strlen(items[i].schema_json) > 4000) FAIL("%s '%s': schema over 4000 characters", what, items[i].name);
        }
    }
    return ESP_OK;
}

esp_err_t homeapps_validate(const homeapps_descriptor_t *d, char *err, size_t err_len)
{
    if (!d || !d->model || !strchr(d->model, '/')) FAIL("model must be author/name");
    if (!d->version) FAIL("version is required");
    if (!d->purpose || (strcmp(d->purpose, "smart_home") && strcmp(d->purpose, "assistant"))) FAIL("purpose must be smart_home or assistant");
    if (d->jarvis && strlen(d->jarvis) > 500) FAIL("jarvis text over 500 characters");
    if (d->heartbeat_s && (d->heartbeat_s < 5 || d->heartbeat_s > 86400)) FAIL("heartbeat must be 5..86400");
    esp_err_t e;
    if ((e = check_list(d->controls, d->controls_len, "control", false, err, err_len)) != ESP_OK) return e;
    if ((e = check_list(d->sensors, d->sensors_len, "sensor", false, err, err_len)) != ESP_OK) return e;
    if ((e = check_list(d->events, d->events_len, "event", false, err, err_len)) != ESP_OK) return e;
    if ((e = check_list(d->actions, d->actions_len, "action", true, err, err_len)) != ESP_OK) return e;
    char *json = homeapps_descriptor_to_json(d);
    size_t n = json ? strlen(json) : 0;
    free(json);
    if (n > HOMEAPPS_MAX_DESCRIPTOR_CHARS) FAIL("descriptor over %d characters", HOMEAPPS_MAX_DESCRIPTOR_CHARS);
    return ESP_OK;
}
