cmake_minimum_required(VERSION 3.20)
project(vice_shim LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# VICE source directory
set(VICE_SOURCE_DIR ${CMAKE_CURRENT_SOURCE_DIR}/vice)

# VICE core sources
set(VICE_SOURCES
    vice/src/main.c
    vice/src/machine.c
    vice/src/cpu.c
    vice/src/vic.c
    vice/src/cia.c
    vice/src/sid.c
    vice/src/memory.c
    vice/src/clock.c
)

# Shim layer
set(SHIM_SOURCES
    vice-shim.c
)

add_library(vice_x64 SHARED
    ${VICE_SOURCES}
    ${SHIM_SOURCES}
)

target_include_directories(vice_x64 PRIVATE
    ${VICE_SOURCE_DIR}/src
    ${CMAKE_CURRENT_SOURCE_DIR}
)

target_compile_definitions(vice_x64 PRIVATE
    HAVE_CONFIG_H
    EMULATOR_BUILD
)

if(WIN32)
    target_compile_definitions(vice_x64 PRIVATE
        _WINDOWS
        WIN32
    )
endif()

set_target_properties(vice_x64 PROPERTIES
    OUTPUT_NAME vice_x64
    PREFIX ""
)