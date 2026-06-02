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

# Upstream Ymir unconditionally builds ymir-sdl3 (apps/) which pulls in
# CURL, SDL3, etc.  We only need ymir::ymir-core from libs/, so we inject
# a no-op apps/CMakeLists.txt via CMAKE_SOURCE_DIR trickery won't work.
# Instead, we temporarily redirect the apps subdirectory to an empty stub
# so the upstream add_subdirectory(apps) is harmless.
set(_ymir_apps_dir "${TELESTO_YMIR_SOURCE_DIR}/apps")
set(_stub_apps_dir "${CMAKE_CURRENT_BINARY_DIR}/stub-apps")
file(MAKE_DIRECTORY "${_stub_apps_dir}")
file(WRITE "${_stub_apps_dir}/CMakeLists.txt" "# stub — skip ymir-sdl3 for the Telesto wrapper build\n")
file(RENAME "${_ymir_apps_dir}/CMakeLists.txt" "${_ymir_apps_dir}/CMakeLists.txt.orig")
file(COPY_FILE "${_stub_apps_dir}/CMakeLists.txt" "${_ymir_apps_dir}/CMakeLists.txt")

add_subdirectory("${TELESTO_YMIR_SOURCE_DIR}" "${CMAKE_BINARY_DIR}/ymir")

# Restore the original apps/CMakeLists.txt so we don't leave the Ymir
# checkout in a modified state.
file(RENAME "${_ymir_apps_dir}/CMakeLists.txt.orig" "${_ymir_apps_dir}/CMakeLists.txt")

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
