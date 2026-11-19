#include <ctype.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>
#include "homeapps_internal.h"

static size_t chars(const char *s)
{
    size_t n = 0;
    for (; *s; s++) if (((unsigned char)*s & 0xC0) != 0x80) n++;   /* characters, not bytes (the server counts characters) */
    return n;
}

/* [a-z][a-z0-9_]{0,max-1} over [s, s+len) */
static bool ident(const char *s, size_t len, size_t max)
{
    if (len < 1 || len > max || !(s[0] >= 'a' && s[0] <= 'z')) return false;
    for (size_t i = 1; i < len; i++) if (!(islower((unsigned char)s[i]) || isdigit((unsigned char)s[i]) || s[i] == '_')) return false;
    return true;
}

/* Field names: ^[a-z][a-z0-9_]{0,29}(\.[a-z][a-z0-9_]{0,29})?$ ; actions: ^[a-z][a-z0-9_]{0,19}$ */
static bool name_ok(const char *n, bool action)
{
    if (!n) return false;
    if (action) return ident(n, strlen(n), 20);
    const char *dot = strchr(n, '.');
    if (!dot) return ident(n, strlen(n), 30);
    return ident(n, (size_t)(dot - n), 30) && ident(dot + 1, strlen(dot + 1), 30);
}

static bool seg_ok(const char *s, size_t len)   /* [a-z0-9][a-z0-9._-]* */
{
    if (!len || !(islower((unsigned char)s[0]) || isdigit((unsigned char)s[0]))) return false;
    for (size_t i = 1; i < len; i++) if (!(islower((unsigned char)s[i]) || isdigit((unsigned char)s[i]) || s[i] == '.' || s[i] == '_' || s[i] == '-')) return false;
    return true;
}

static bool model_ok(const char *m)
{
    size_t n = strlen(m);
    if (n > 100) return false;
    const char *slash = strchr(m, '/');
    if (!slash) return seg_ok(m, n);
    return seg_ok(m, (size_t)(slash - m)) && !strchr(slash + 1, '/') && seg_ok(slash + 1, strlen(slash + 1));
}

/* ^\d{1,4}(\.\d{1,4}){0,2}([-+][0-9A-Za-z.-]{1,30})?$ */
static bool version_ok(const char *v)
{
    for (int part = 0; part < 3; part++) {
        int d = 0;
        while (isdigit((unsigned char)*v) && d < 5) { v++; d++; }
        if (d < 1 || d > 4) return false;
        if (*v != '.') break;
        v++;
        if (part == 2) return false;
    }
    if (!*v) return true;
    if (*v != '-' && *v != '+') return false;
    v++;
    size_t n = strlen(v);
    if (n < 1 || n > 30) return false;
    for (; *v; v++) if (!(isalnum((unsigned char)*v) || *v == '.' || *v == '-')) return false;
    return true;
}

/* ^[a-z][a-z0-9_]{0,29}$ or ^[a-z0-9][a-z0-9-]{0,29}/[a-z][a-z0-9_-]{0,29}$ */
static bool type_ok(const char *t)
{
    const char *slash = strchr(t, '/');
    if (!slash) return ident(t, strlen(t), 30);
    size_t al = (size_t)(slash - t), nl = strlen(slash + 1);
    if (al < 1 || al > 30 || nl < 1 || nl > 30 || !(islower((unsigned char)t[0]) || isdigit((unsigned char)t[0])) || !islower((unsigned char)slash[1])) return false;
    for (size_t i = 1; i < al; i++) if (!(islower((unsigned char)t[i]) || isdigit((unsigned char)t[i]) || t[i] == '-')) return false;
    for (size_t i = 1; i < nl; i++) { char c = slash[1 + i]; if (!(islower((unsigned char)c) || isdigit((unsigned char)c) || c == '_' || c == '-')) return false; }
    return true;
}

static bool value_schema_ok(const cJSON *sc)
{
    if (!cJSON_IsObject(sc)) return false;
    const cJSON *type = cJSON_GetObjectItem(sc, "type");
    if (type) {
        if (!cJSON_IsString(type)) return false;
        static const char *const types[] = { "boolean", "integer", "number", "string", "object", "array", "null" };
        for (size_t i = 0; i < sizeof(types) / sizeof(*types); i++) if (!strcmp(type->valuestring, types[i])) return true;
        return false;
    }
    return cJSON_GetObjectItem(sc, "enum") || cJSON_GetObjectItem(sc, "const");
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

/* kind: 0 control, 1 sensor, 2 event, 3 action (same as add_items) */
static esp_err_t check_list(const homeapps_item_t *items, size_t len, const char *what, int kind, char *err, size_t err_len)
{
    if (len > HOMEAPPS_MAX_ITEMS) FAIL("%s: more than %d items", what, HOMEAPPS_MAX_ITEMS);
    for (size_t i = 0; i < len; i++) {
        const homeapps_item_t *it = &items[i];
        if (!name_ok(it->name, kind == 3)) FAIL("%s: bad name '%s'", what, it->name ? it->name : "(null)");
        if (it->label && chars(it->label) > 60) FAIL("%s '%s': label over 60 characters", what, it->name);
        if (it->description && chars(it->description) > 300) FAIL("%s '%s': description over 300 characters", what, it->name);
        if (it->unit && chars(it->unit) > 20) FAIL("%s '%s': unit over 20 characters", what, it->name);
        if (kind == 0 && it->widget && strcmp(it->widget, "switch") && strcmp(it->widget, "slider") && strcmp(it->widget, "select") && strcmp(it->widget, "button"))
            FAIL("%s '%s': widget must be switch, slider, select or button", what, it->name);
        bool needs_schema = kind == 0 || kind == 1;   /* an action without input is valid */
        if (!it->schema_json) {
            if (needs_schema) FAIL("%s '%s': schema is required", what, it->name);
            continue;
        }
        if (strlen(it->schema_json) > 4000) FAIL("%s '%s': schema over 4000 characters", what, it->name);
        cJSON *p = cJSON_Parse(it->schema_json);
        if (!p) FAIL("%s '%s': schema is not valid JSON", what, it->name);
        bool ok = kind == 3 ? (value_schema_ok(p) && cJSON_IsString(cJSON_GetObjectItem(p, "type")) && !strcmp(cJSON_GetObjectItem(p, "type")->valuestring, "object")) : value_schema_ok(p);
        cJSON_Delete(p);
        if (!ok) FAIL("%s '%s': schema needs a valid type (an action: type object) or an enum/const", what, it->name);
    }
    return ESP_OK;
}

static bool dup_in(const homeapps_item_t *a, size_t an, const homeapps_item_t *b, size_t bn, const char **name)
{
    for (size_t i = 0; i < an; i++) {
        for (size_t j = (a == b ? i + 1 : 0); j < bn; j++)
            if (a[i].name && b[j].name && !strcmp(a[i].name, b[j].name)) { *name = a[i].name; return true; }
    }
    return false;
}

/* [{"type":"weather"},{"type":"switch","id":"lamp"},{"type":"author/name","definition":{...}}] */
static esp_err_t check_capabilities(const char *json, char *err, size_t err_len)
{
    if (!json) return ESP_OK;
    cJSON *arr = cJSON_Parse(json);
    if (!arr || !cJSON_IsArray(arr) || cJSON_GetArraySize(arr) > HOMEAPPS_MAX_ITEMS) { cJSON_Delete(arr); FAIL("capabilities must be a JSON array of at most %d items", HOMEAPPS_MAX_ITEMS); }
    int n = cJSON_GetArraySize(arr);
    for (int i = 0; i < n; i++) {
        const cJSON *c = cJSON_GetArrayItem(arr, i);
        const cJSON *type = cJSON_GetObjectItem(c, "type"), *id = cJSON_GetObjectItem(c, "id"), *def = cJSON_GetObjectItem(c, "definition");
        bool ok = cJSON_IsObject(c) && cJSON_IsString(type) && type_ok(type->valuestring);
        if (ok && id) ok = cJSON_IsString(id) && ident(id->valuestring, strlen(id->valuestring), 30);
        if (ok && def) ok = cJSON_IsObject(def);
        if (!ok) { cJSON_Delete(arr); FAIL("capability %d needs a type (weather or author/name), an optional id and an object definition", i); }
        for (int j = 0; j < i; j++) {
            const cJSON *o = cJSON_GetArrayItem(arr, j), *ot = cJSON_GetObjectItem(o, "type"), *oi = cJSON_GetObjectItem(o, "id");
            if (!strcmp(ot->valuestring, type->valuestring) && !strcmp(oi ? oi->valuestring : "", id ? id->valuestring : "")) { if (err) snprintf(err, err_len, "capability '%s' is declared twice", type->valuestring); cJSON_Delete(arr); return ESP_ERR_INVALID_ARG; }
        }
    }
    cJSON_Delete(arr);
    return ESP_OK;
}

esp_err_t homeapps_validate(const homeapps_descriptor_t *d, char *err, size_t err_len)
{
    if (!d || !d->model || !model_ok(d->model)) FAIL("model must be lowercase author/name, at most 100 characters");
    if (!d->version || !version_ok(d->version)) FAIL("version must look like 1.2.0");
    if (!d->purpose || (strcmp(d->purpose, "smart_home") && strcmp(d->purpose, "assistant") && strcmp(d->purpose, "computer"))) FAIL("purpose must be assistant, smart_home or computer");
    if (d->jarvis && chars(d->jarvis) > 500) FAIL("jarvis text over 500 characters");
    if (d->heartbeat_s && (d->heartbeat_s < 5 || d->heartbeat_s > 86400)) FAIL("heartbeat must be 5..86400");
    size_t roles = 0;
    for (; d->roles && d->roles[roles]; roles++) {
        if (roles >= 10) FAIL("more than 10 roles");
        if (!ident(d->roles[roles], strlen(d->roles[roles]), 40)) FAIL("bad role '%s'", d->roles[roles]);
    }
    esp_err_t e;
    if ((e = check_list(d->controls, d->controls_len, "control", 0, err, err_len)) != ESP_OK) return e;
    if ((e = check_list(d->sensors, d->sensors_len, "sensor", 1, err, err_len)) != ESP_OK) return e;
    if ((e = check_list(d->events, d->events_len, "event", 2, err, err_len)) != ESP_OK) return e;
    if ((e = check_list(d->actions, d->actions_len, "action", 3, err, err_len)) != ESP_OK) return e;
    if ((e = check_capabilities(d->capabilities_json, err, err_len)) != ESP_OK) return e;
    const char *dup;
    if (dup_in(d->controls, d->controls_len, d->controls, d->controls_len, &dup) || dup_in(d->sensors, d->sensors_len, d->sensors, d->sensors_len, &dup) ||
        dup_in(d->controls, d->controls_len, d->sensors, d->sensors_len, &dup)) FAIL("control or sensor '%s' is declared twice", dup);
    if (dup_in(d->events, d->events_len, d->events, d->events_len, &dup)) FAIL("event '%s' is declared twice", dup);
    if (dup_in(d->actions, d->actions_len, d->actions, d->actions_len, &dup)) FAIL("action '%s' is declared twice", dup);
    char *json = homeapps_descriptor_to_json(d);
    size_t n = json ? chars(json) : 0;
    free(json);
    if (n > HOMEAPPS_MAX_DESCRIPTOR_CHARS) FAIL("descriptor over %d characters", HOMEAPPS_MAX_DESCRIPTOR_CHARS);
    return ESP_OK;
}
