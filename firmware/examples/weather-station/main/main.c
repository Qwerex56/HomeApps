/* Battery weather station (target: ESP32-C6): wakes, POSTs /devices/report, deep sleeps. */
#include <stdio.h>
#include "esp_sleep.h"
#include "homeapps_device.h"

#define WAKE_S 900

static const char *const roles[] = { "weather_station", NULL };
static const homeapps_item_t sensors[] = {
    { .name = "temperature", .label = "Temperature", .schema_json = "{\"type\":\"number\"}", .unit = "C" },
    { .name = "humidity", .label = "Humidity", .schema_json = "{\"type\":\"number\",\"minimum\":0,\"maximum\":100}", .unit = "%" },
    { .name = "battery", .label = "Battery", .schema_json = "{\"type\":\"number\",\"minimum\":0,\"maximum\":100}", .unit = "%" } };
static const homeapps_descriptor_t desc = {
    .model = "example/weather-c6", .version = "0.1.0", .purpose = "smart_home", .roles = roles, .sleepy = true, .heartbeat_s = WAKE_S,
    .capabilities_json = "[{\"type\":\"weather\"},{\"type\":\"power\"}]", .jarvis = "Outdoor weather station on a battery.",
    .sensors = sensors, .sensors_len = 3 };

void app_main(void)
{
    homeapps_config_t cfg = { .descriptor = &desc };
    /* On failure (ESP_ERR_TIMEOUT: no Wi-Fi or server) skip reporting and sleep anyway; try again at the next wake. */
    if (homeapps_start(&cfg) == ESP_OK) {
        float t = 21.5f, h = 48.0f; int battery = 80; /* replace with real sensor readings */
        char values[128];
        snprintf(values, sizeof(values), "{\"temperature\":%.1f,\"humidity\":%.0f,\"battery\":%d}", t, h, battery);
        homeapps_send_state(values);
        homeapps_send_status(battery, 0, NULL);
        homeapps_flush();
    }
    esp_deep_sleep(WAKE_S * 1000000ULL);
}
