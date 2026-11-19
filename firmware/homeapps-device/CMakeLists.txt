idf_component_register(
    SRCS "src/homeapps_device.c" "src/homeapps_descriptor.c" "src/homeapps_setup.c" "src/homeapps_store.c"
    INCLUDE_DIRS "include"
    PRIV_INCLUDE_DIRS "src"
    PRIV_REQUIRES bootloader_support
    REQUIRES nvs_flash esp_wifi esp_netif esp_event esp_http_client esp_http_server esp_websocket_client esp_timer json mbedtls esp-tls driver lwip
    EMBED_TXTFILES "${CMAKE_CURRENT_LIST_DIR}/pinned_ca.pem" )

if(CONFIG_HOMEAPPS_TLS_PINNED_CA)
    file(READ "${CMAKE_CURRENT_LIST_DIR}/pinned_ca.pem" _ha_ca)
    string(FIND "${_ha_ca}" "-----BEGIN CERTIFICATE-----" _ha_pos)
    if(_ha_pos EQUAL -1)
        message(FATAL_ERROR "homeapps-device: HOMEAPPS_TLS_PINNED_CA is on but firmware/homeapps-device/pinned_ca.pem is still the placeholder. Put your CA certificate (PEM) in it.")
    endif()
endif()
