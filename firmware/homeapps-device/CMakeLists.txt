idf_component_register(
    SRCS "src/homeapps_device.c" "src/homeapps_descriptor.c" "src/homeapps_setup.c" "src/homeapps_store.c"
    INCLUDE_DIRS "include"
    PRIV_INCLUDE_DIRS "src"
    REQUIRES nvs_flash esp_wifi esp_netif esp_event esp_http_client esp_http_server esp_websocket_client esp_timer esp_https_ota json mbedtls esp-tls driver lwip
    EMBED_TXTFILES "${CMAKE_CURRENT_LIST_DIR}/pinned_ca.pem" )
