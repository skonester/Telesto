cmake_minimum_required(VERSION 3.28)

project(telesto-ymir-core LANGUAGES CXX)

set(TELESTO_YMIR_SOURCE_DIR "" CACHE PATH "Path to a local StrikerX3/Ymir checkout")

if (NOT TELESTO_YMIR_SOURCE_DIR)
    message(FATAL_ERROR "Set TELESTO_YMIR_SOURCE_DIR to a local StrikerX3/Ymir checkout")
endif ()

set(Ymir_ENABLE_SANDBOX OFF CACHE BOOL "" FORCE)
set(Ymir_ENABLE_YMDASM OFF CACHE BOOL "" FORCE)
set(Ymir_ENABLE_YMIR_HEADLESS OFF CACHE BOOL "" FORCE)
set(Ymir_ENABLE_YMIR_DBG OFF CACHE BOOL "" FORCE)
set(Ymir_ENABLE_TESTS OFF CACHE BOOL "" FORCE)
set(Ymir_ENABLE_UPDATE_CHECKS OFF CACHE BOOL "" FORCE)
set(Ymir_ENABLE_IMGUI_DEMO OFF CACHE BOOL "" FORCE)
set(Ymir_ENABLE_DEVLOG OFF CACHE BOOL "" FORCE)
set(Ymir_ENABLE_DEV_ASSERTIONS OFF CACHE BOOL "" FORCE)
set(Ymir_DEV_BUILD OFF CACHE BOOL "" FORCE)
set(WITH_LZMA_ASM OFF CACHE BOOL "" FORCE)

add_subdirectory("${TELESTO_YMIR_SOURCE_DIR}" "${CMAKE_BINARY_DIR}/ymir")

find_package(cereal CONFIG REQUIRED)
find_package(fmt CONFIG REQUIRED)

add_library(telesto-ymir-core SHARED
    include/telesto_ymir_core.h
    src/telesto_ymir_core.cpp
)

add_library(telesto::ymir-core ALIAS telesto-ymir-core)

target_compile_features(telesto-ymir-core PRIVATE cxx_std_20)

target_include_directories(telesto-ymir-core
    PUBLIC
        "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>"
    PRIVATE
        "${TELESTO_YMIR_SOURCE_DIR}/apps/ymir-sdl3/src"
)

target_link_libraries(telesto-ymir-core
    PRIVATE
        ymir::ymir-core
        cereal::cereal
        fmt::fmt
)

target_compile_definitions(telesto-ymir-core
    PRIVATE
        TELESTO_YMIR_CORE_EXPORTS
)

if (MSVC)
    target_compile_options(telesto-ymir-core PRIVATE /W4 /permissive-)
else ()
    target_compile_options(telesto-ymir-core PRIVATE -Wall -Wextra -Wpedantic)
endif ()
