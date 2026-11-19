/* Presence tag (any target): declares the "geo" capability; sends "seen" events (location events, never shown to guests). */
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "homeapps_device.h"

static const char *const roles[] = { "tracker", NULL };
static const homeapps_item_t sensors[] = {
    { .name = "location", .label = "Location", .schema_json = "{\"type\":\"object\"}" } };
static const homeapps_item_t events[] = {
    { .name = "seen", .label = "Seen", .schema_json = "{\"type\":\"object\"}", .notify = false } };
static const homeapps_descriptor_t desc = {
    .model = "example/presence-tag", .version = "0.1.0", .purpose = "smart_home", .roles = roles,
    .capabilities_json = "[{\"type\":\"geo\"}]", .jarvis = "A tag that reports where it is seen.",
    .sensors = sensors, .sensors_len = 1, .events = events, .events_len = 1 };

void app_main(void)
{
    homeapps_config_t cfg = { .descriptor = &desc };
    if (homeapps_start(&cfg) != ESP_OK) return;
    for (;;) {
        homeapps_send_event("seen", "{\"where\":\"hall\"}");
        vTaskDelay(pdMS_TO_TICKS(60000));
    }
}
