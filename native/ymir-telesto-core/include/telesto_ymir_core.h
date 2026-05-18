#pragma once

#include <stdint.h>

#if defined(_WIN32)
#  if defined(TELESTO_YMIR_CORE_EXPORTS)
#    define TELESTO_YMIR_API __declspec(dllexport)
#  else
#    define TELESTO_YMIR_API __declspec(dllimport)
#  endif
#else
#  define TELESTO_YMIR_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct TelestoYmirContext TelestoYmirContext;

typedef enum TelestoYmirResult {
    TELESTO_YMIR_OK = 0,
    TELESTO_YMIR_INVALID_ARGUMENT = 1,
    TELESTO_YMIR_FILE_NOT_FOUND = 2,
    TELESTO_YMIR_INVALID_IPL = 3,
    TELESTO_YMIR_DISC_LOAD_FAILED = 4,
    TELESTO_YMIR_CORE_ERROR = 5
} TelestoYmirResult;

typedef enum TelestoYmirButton {
    TELESTO_YMIR_BUTTON_RIGHT = 1u << 15u,
    TELESTO_YMIR_BUTTON_LEFT  = 1u << 14u,
    TELESTO_YMIR_BUTTON_DOWN  = 1u << 13u,
    TELESTO_YMIR_BUTTON_UP    = 1u << 12u,
    TELESTO_YMIR_BUTTON_START = 1u << 11u,
    TELESTO_YMIR_BUTTON_A     = 1u << 10u,
    TELESTO_YMIR_BUTTON_C     = 1u << 9u,
    TELESTO_YMIR_BUTTON_B     = 1u << 8u,
    TELESTO_YMIR_BUTTON_R     = 1u << 7u,
    TELESTO_YMIR_BUTTON_X     = 1u << 6u,
    TELESTO_YMIR_BUTTON_Y     = 1u << 5u,
    TELESTO_YMIR_BUTTON_Z     = 1u << 4u,
    TELESTO_YMIR_BUTTON_L     = 1u << 3u
} TelestoYmirButton;

typedef void (*TelestoYmirVideoCallback)(
    void *user_data,
    const uint32_t *xrgb8888,
    uint32_t width,
    uint32_t height);

typedef void (*TelestoYmirAudioCallback)(
    void *user_data,
    int16_t left,
    int16_t right);

TELESTO_YMIR_API TelestoYmirContext *telesto_ymir_create(void);
TELESTO_YMIR_API void telesto_ymir_destroy(TelestoYmirContext *ctx);

TELESTO_YMIR_API const char *telesto_ymir_last_error(TelestoYmirContext *ctx);

TELESTO_YMIR_API void telesto_ymir_set_video_callback(
    TelestoYmirContext *ctx,
    TelestoYmirVideoCallback callback,
    void *user_data);

TELESTO_YMIR_API void telesto_ymir_set_audio_callback(
    TelestoYmirContext *ctx,
    TelestoYmirAudioCallback callback,
    void *user_data);

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_load_ipl(
    TelestoYmirContext *ctx,
    const char *path_utf8);

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_load_disc(
    TelestoYmirContext *ctx,
    const char *path_utf8);

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_load_internal_backup_ram(
    TelestoYmirContext *ctx,
    const char *path_utf8);

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_insert_backup_ram_cartridge(
    TelestoYmirContext *ctx,
    const char *path_utf8);

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_save_state(
    TelestoYmirContext *ctx,
    const char *path_utf8);

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_load_state(
    TelestoYmirContext *ctx,
    const char *path_utf8);

TELESTO_YMIR_API void telesto_ymir_reset(TelestoYmirContext *ctx, int hard);
TELESTO_YMIR_API void telesto_ymir_run_frame(TelestoYmirContext *ctx);

TELESTO_YMIR_API void telesto_ymir_set_control_pad_state(
    TelestoYmirContext *ctx,
    uint32_t port,
    uint16_t pressed_buttons);

#ifdef __cplusplus
}
#endif
