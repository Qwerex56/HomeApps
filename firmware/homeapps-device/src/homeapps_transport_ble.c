/* BLE transport stub: the GATT layout and the TLS-over-GATT design are in docs/ble-bridge.md. Nothing here touches NimBLE
 * yet; it only exists so the seam can be selected (Kconfig HOMEAPPS_TRANSPORT_BLE) and fails loudly. */
#include "esp_err.h"
#include "homeapps_internal.h"

static esp_err_t ble_open(void) { return ESP_ERR_NOT_SUPPORTED; }

static int ble_post_json(const char *path, const char *bearer, const char *body, http_out_t *out)
{
    (void)path; (void)bearer; (void)body; (void)out;
    return -1;   /* not implemented */
}

const homeapps_transport_t homeapps_transport_ble = { .name = "ble", .open = ble_open, .post_json = ble_post_json };
