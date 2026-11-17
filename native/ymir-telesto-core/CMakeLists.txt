cmake_minimum_required(VERSION 3.28)

project(telesto-ymir-core LANGUAGES CXX)

set(TELESTO_YMIR_SOURCE_DIR "" CACHE PATH "Path to a local ymir-emu/Ymir checkout (v0.4.1 or newer)")

if (NOT TELESTO_YMIR_SOURCE_DIR)
    message(FATAL_ERROR "Set TELESTO_YMIR_SOURCE_DIR to a local ymir-emu/Ymir checkout")
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
set(Ymir_LIBRARY_ONLY ON CACHE BOOL "" FORCE)

# Upstream supports library-only embedding; leave its source tree untouched.
add_subdirectory("${TELESTO_YMIR_SOURCE_DIR}" "${CMAKE_BINARY_DIR}/ymir")

add_library(telesto-ymir-core SHARED
    include/telesto_ymir_core.h
    src/telesto_ymir_core.cpp
)

add_library(telesto::ymir-core ALIAS telesto-ymir-core)

target_compile_features(telesto-ymir-core PRIVATE cxx_std_20)

target_include_directories(telesto-ymir-core
    PUBLIC
        "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>"
)

target_link_libraries(telesto-ymir-core
    PRIVATE
        ymir::ymir-core
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

include(CTest)
if (BUILD_TESTING)
    add_executable(telesto-ymir-smoke tests/smoke.cpp)
    target_link_libraries(telesto-ymir-smoke PRIVATE telesto::ymir-core)
    add_test(NAME telesto-ymir-smoke COMMAND telesto-ymir-smoke)
    set_tests_properties(telesto-ymir-smoke PROPERTIES TIMEOUT 60)
endif ()
