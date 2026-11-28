/* C3 Power Lab (target: ESP32-C3): random weather readings in one of three power modes, for current measurements.
 * Not build-tested locally. Procedure and metrics: docs/c3-power-lab.md. */
#include <stdio.h>
#include <stdlib.h>
#include "esp_log.h"
#include "esp_random.h"
#include "esp_sleep.h"
#include "esp_timer.h"
#include "esp_wifi.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "homeapps_device.h"

#define PERIOD_S CONFIG_POWERLAB_PERIOD_S
#ifdef CONFIG_POWERLAB_MODE_DEEP_SLEEP
#define SLEEPY true
#else
#define SLEEPY false
#endif

static const char *TAG = "powerlab";
static const char *const roles[] = { "weather_station", NULL };
static const homeapps_item_t sensors[] = {
    { .name = "temperature", .label = "Temperature", .schema_json = "{\"type\":\"number\"}", .unit = "C" },
    { .name = "humidity", .label = "Humidity", .schema_json = "{\"type\":\"number\",\"minimum\":0,\"maximum\":100}", .unit = "%" },
    { .name = "pressure", .label = "Pressure", .schema_json = "{\"type\":\"number\"}", .unit = "hPa" },
    { .name = "battery", .label = "Battery", .schema_json = "{\"type\":\"number\",\"minimum\":0,\"maximum\":100}", .unit = "%" } };
static const homeapps_descriptor_t desc = {
    .model = "homeapps/c3-power-lab", .version = "0.1.0", .purpose = "smart_home", .roles = roles, .sleepy = SLEEPY,
    .heartbeat_s = PERIOD_S, .capabilities_json = "[{\"type\":\"weather\"},{\"type\":\"power\"}]",
    .jarvis = "Test weather board used to measure power modes.", .sensors = sensors, .sensors_len = 4 };

/* Returns ESP_OK when the report was taken. Logs the time from start of the cycle to the end of the send. */
static esp_err_t report(int64_t cycle_start_us)
{
    int battery = 20 + (int)(esp_random() % 81);
    float t = 15.0f + (esp_random() % 150) / 10.0f, h = 30.0f + (esp_random() % 600) / 10.0f, p = 980.0f + (esp_random() % 400) / 10.0f;
    char values[160];
    snprintf(values, sizeof(values), "{\"temperature\":%.1f,\"humidity\":%.0f,\"pressure\":%.1f,\"battery\":%d}", t, h, p, battery);
    esp_err_t e = homeapps_send_state(values);
    if (e == ESP_OK) e = homeapps_send_status(battery, 0, NULL);
    if (e == ESP_OK && SLEEPY) e = homeapps_flush();
    ESP_LOGI(TAG, "report %s wake_to_send_ms=%lld", e == ESP_OK ? "OK" : "FAIL", (long long)((esp_timer_get_time() - cycle_start_us) / 1000));
    return e;
}

void app_main(void)
{
    int64_t t0 = esp_timer_get_time();
    homeapps_config_t cfg = { .descriptor = &desc };
    esp_err_t e = homeapps_start(&cfg);
#ifdef CONFIG_POWERLAB_MODE_DEEP_SLEEP
    /* Failure (no Wi-Fi or server): sleep anyway, the next wake is the retry. */
    if (e == ESP_OK) report(t0); else ESP_LOGW(TAG, "start failed (%s), report skipped", esp_err_to_name(e));
    esp_deep_sleep(PERIOD_S * 1000000ULL);
#else
    if (e != ESP_OK) ESP_LOGW(TAG, "start failed (%s)", esp_err_to_name(e));
#ifdef CONFIG_POWERLAB_MODE_MODEM_SLEEP
    esp_wifi_set_ps(WIFI_PS_MIN_MODEM);
#else
    esp_wifi_set_ps(WIFI_PS_NONE);
#endif
    for (;;) {
        vTaskDelay(pdMS_TO_TICKS(PERIOD_S * 1000));
        report(esp_timer_get_time());
    }
#endif
}
