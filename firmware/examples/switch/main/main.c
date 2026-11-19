/* Switch (target: ESP32-C3): one relay, one "switch" capability, button reports "pressed". */
#include "driver/gpio.h"
#include "homeapps_device.h"

#define RELAY_GPIO 4

static const char *const roles[] = { "light", NULL };
static const homeapps_item_t controls[] = {
    { .name = "on", .label = "Light", .schema_json = "{\"type\":\"boolean\"}", .widget = "switch" } };
static const homeapps_descriptor_t desc = {
    .model = "example/switch-c3", .version = "0.1.0", .purpose = "smart_home", .roles = roles,
    .capabilities_json = "[{\"type\":\"switch\"}]", .jarvis = "A light on a relay. On or off.",
    .controls = controls, .controls_len = 1 };

static esp_err_t on_set(const char *name, const char *value_json, void *user)
{
    (void)user;
    bool on = value_json[0] == 't';
    gpio_set_level(RELAY_GPIO, on);
    homeapps_send_state(on ? "{\"on\":true}" : "{\"on\":false}");
    (void)name;
    return ESP_OK;
}

void app_main(void)
{
    gpio_set_direction(RELAY_GPIO, GPIO_MODE_OUTPUT);
    homeapps_config_t cfg = { .descriptor = &desc, .on_set = on_set };
    if (homeapps_start(&cfg) == ESP_OK) homeapps_send_state("{\"on\":false}");
}
