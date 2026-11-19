cmake_minimum_required(VERSION 3.16)
set(EXTRA_COMPONENT_DIRS "${CMAKE_CURRENT_LIST_DIR}/../../homeapps-device")
include($ENV{IDF_PATH}/tools/cmake/project.cmake)
project(homeapps_weather_station)
