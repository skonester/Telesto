#include "telesto_ymir_core.h"

#include <ymir/ymir.hpp>
#include <ymir/hw/cart/cart_impl_bup.hpp>

#include <serdes/cereal_savestate.hpp>

#include <cereal/archives/portable_binary.hpp>
#include <fmt/format.h>

#include <array>
#include <exception>
#include <filesystem>
#include <fstream>
#include <span>
#include <string>
#include <system_error>
#include <utility>
#include <vector>

namespace {

constexpr auto kAllButtons =
    static_cast<uint16_t>(ymir::peripheral::Button::Default);

std::filesystem::path FromUtf8Path(const char *path) {
    return std::filesystem::u8path(path ? path : "");
}

bool ReadExactFile(const std::filesystem::path &path, uint8_t *buffer, size_t size) {
    std::ifstream in(path, std::ios::binary);
    if (!in) {
        return false;
    }

    in.read(reinterpret_cast<char *>(buffer), static_cast<std::streamsize>(size));
    return in.gcount() == static_cast<std::streamsize>(size);
}

} // namespace

struct TelestoYmirContext {
    ymir::Saturn saturn;

    TelestoYmirVideoCallback videoCallback = nullptr;
    void *videoUserData = nullptr;

    TelestoYmirAudioCallback audioCallback = nullptr;
    void *audioUserData = nullptr;

    std::array<uint16_t, 2> pressedButtons{};
    std::string lastError;

    static void OnVideoFrame(uint32 *fb, uint32 width, uint32 height, void *userData) {
        auto *ctx = static_cast<TelestoYmirContext *>(userData);
        if (ctx && ctx->videoCallback) {
            ctx->videoCallback(ctx->videoUserData, reinterpret_cast<const uint32_t *>(fb), width, height);
        }
    }

    static void OnAudioSample(sint16 left, sint16 right, void *userData) {
        auto *ctx = static_cast<TelestoYmirContext *>(userData);
        if (ctx && ctx->audioCallback) {
            ctx->audioCallback(ctx->audioUserData, left, right);
        }
    }

    static void ReportControlPad(TelestoYmirContext *ctx, uint32_t port, ymir::peripheral::PeripheralReport &out) {
        const uint16_t pressed = port < ctx->pressedButtons.size() ? ctx->pressedButtons[port] : 0;
        out.type = ymir::peripheral::PeripheralType::ControlPad;
        out.report.controlPad.buttons =
            static_cast<ymir::peripheral::Button>(kAllButtons & static_cast<uint16_t>(~pressed));
    }

    static void OnPort1Report(ymir::peripheral::PeripheralReport &out, void *userData) {
        if (auto *ctx = static_cast<TelestoYmirContext *>(userData)) {
            ReportControlPad(ctx, 0, out);
        }
    }

    static void OnPort2Report(ymir::peripheral::PeripheralReport &out, void *userData) {
        if (auto *ctx = static_cast<TelestoYmirContext *>(userData)) {
            ReportControlPad(ctx, 1, out);
        }
    }

    TelestoYmirContext() {
        saturn.configuration.video.threadedVDP1 = true;
        saturn.configuration.video.threadedVDP2 = true;
        saturn.configuration.video.threadedDeinterlacer = true;
        saturn.configuration.system.emulateSH2Cache = false;
        saturn.configuration.cdblock.useLLE = false;

        saturn.VDP.UseSoftwareRenderer();
        saturn.VDP.SetSoftwareRenderCallback({this, OnVideoFrame});

        saturn.SCSP.SetSampleCallback({this, OnAudioSample});

        saturn.SMPC.GetPeripheralPort1().ConnectControlPad();
        saturn.SMPC.GetPeripheralPort1().SetPeripheralReportCallback({this, OnPort1Report});

        saturn.SMPC.GetPeripheralPort2().ConnectControlPad();
        saturn.SMPC.GetPeripheralPort2().SetPeripheralReportCallback({this, OnPort2Report});
    }

    TelestoYmirResult Fail(TelestoYmirResult result, std::string message) {
        lastError = std::move(message);
        return result;
    }
};

extern "C" {

TELESTO_YMIR_API TelestoYmirContext *telesto_ymir_create(void) {
    try {
        return new TelestoYmirContext();
    } catch (...) {
        return nullptr;
    }
}

TELESTO_YMIR_API void telesto_ymir_destroy(TelestoYmirContext *ctx) {
    delete ctx;
}

TELESTO_YMIR_API const char *telesto_ymir_last_error(TelestoYmirContext *ctx) {
    if (!ctx) {
        return "Ymir context is null";
    }
    return ctx->lastError.c_str();
}

TELESTO_YMIR_API void telesto_ymir_set_video_callback(
    TelestoYmirContext *ctx,
    TelestoYmirVideoCallback callback,
    void *user_data) {
    if (!ctx) {
        return;
    }
    ctx->videoCallback = callback;
    ctx->videoUserData = user_data;
}

TELESTO_YMIR_API void telesto_ymir_set_audio_callback(
    TelestoYmirContext *ctx,
    TelestoYmirAudioCallback callback,
    void *user_data) {
    if (!ctx) {
        return;
    }
    ctx->audioCallback = callback;
    ctx->audioUserData = user_data;
}

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_load_ipl(
    TelestoYmirContext *ctx,
    const char *path_utf8) {
    if (!ctx || !path_utf8) {
        return TELESTO_YMIR_INVALID_ARGUMENT;
    }

    try {
        const auto path = FromUtf8Path(path_utf8);
        if (!std::filesystem::exists(path)) {
            return ctx->Fail(TELESTO_YMIR_FILE_NOT_FOUND, "IPL ROM file not found");
        }

        std::array<uint8, ymir::sys::kIPLSize> ipl{};
        if (!ReadExactFile(path, ipl.data(), ipl.size())) {
            return ctx->Fail(TELESTO_YMIR_INVALID_IPL, "IPL ROM is not the expected size");
        }

        ctx->saturn.LoadIPL(std::span<uint8, ymir::sys::kIPLSize>(ipl));
        return TELESTO_YMIR_OK;
    } catch (const std::exception &ex) {
        return ctx->Fail(TELESTO_YMIR_CORE_ERROR, ex.what());
    }
}

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_load_disc(
    TelestoYmirContext *ctx,
    const char *path_utf8) {
    if (!ctx || !path_utf8) {
        return TELESTO_YMIR_INVALID_ARGUMENT;
    }

    try {
        const auto path = FromUtf8Path(path_utf8);
        if (!std::filesystem::exists(path)) {
            return ctx->Fail(TELESTO_YMIR_FILE_NOT_FOUND, "Disc image file not found");
        }

        ymir::media::Disc disc;
        std::vector<std::string> messages;
        const bool loaded = ymir::media::LoadDisc(
            path,
            disc,
            false,
            [&](ymir::media::MessageType, std::string message) { messages.emplace_back(std::move(message)); });

        if (!loaded) {
            std::string error = "Ymir could not load the disc image";
            if (!messages.empty()) {
                error += ": " + messages.back();
            }
            return ctx->Fail(TELESTO_YMIR_DISC_LOAD_FAILED, std::move(error));
        }

        ctx->saturn.LoadDisc(std::move(disc));
        ctx->saturn.Reset(true);
        ctx->saturn.CloseTray();
        return TELESTO_YMIR_OK;
    } catch (const std::exception &ex) {
        return ctx->Fail(TELESTO_YMIR_CORE_ERROR, ex.what());
    }
}

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_load_internal_backup_ram(
    TelestoYmirContext *ctx,
    const char *path_utf8) {
    if (!ctx || !path_utf8) {
        return TELESTO_YMIR_INVALID_ARGUMENT;
    }

    try {
        std::error_code error;
        ctx->saturn.LoadInternalBackupMemoryImage(FromUtf8Path(path_utf8), false, error);
        if (error) {
            return ctx->Fail(TELESTO_YMIR_CORE_ERROR, error.message());
        }
        return TELESTO_YMIR_OK;
    } catch (const std::exception &ex) {
        return ctx->Fail(TELESTO_YMIR_CORE_ERROR, ex.what());
    }
}

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_insert_backup_ram_cartridge(
    TelestoYmirContext *ctx,
    const char *path_utf8) {
    if (!ctx || !path_utf8) {
        return TELESTO_YMIR_INVALID_ARGUMENT;
    }

    try {
        std::error_code error;
        ymir::bup::BackupMemory backupRam{};
        backupRam.CreateFrom(
            FromUtf8Path(path_utf8),
            false,
            error,
            ymir::bup::BackupMemorySize::_32Mbit);

        if (error) {
            return ctx->Fail(TELESTO_YMIR_CORE_ERROR, error.message());
        }

        ctx->saturn.InsertCartridge<ymir::cart::BackupMemoryCartridge>(std::move(backupRam));
        return TELESTO_YMIR_OK;
    } catch (const std::exception &ex) {
        return ctx->Fail(TELESTO_YMIR_CORE_ERROR, ex.what());
    }
}

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_save_state(
    TelestoYmirContext *ctx,
    const char *path_utf8) {
    if (!ctx || !path_utf8) {
        return TELESTO_YMIR_INVALID_ARGUMENT;
    }

    try {
        const auto path = FromUtf8Path(path_utf8);
        if (path.has_parent_path()) {
            std::filesystem::create_directories(path.parent_path());
        }

        ymir::savestate::SaveState state{};
        ctx->saturn.SaveState(state);

        std::ofstream out{path, std::ios::binary};
        if (!out) {
            return ctx->Fail(TELESTO_YMIR_CORE_ERROR, "Could not open save state file for writing");
        }

        cereal::PortableBinaryOutputArchive archive{out};
        archive(state);
        return TELESTO_YMIR_OK;
    } catch (const std::exception &ex) {
        return ctx->Fail(TELESTO_YMIR_CORE_ERROR, ex.what());
    }
}

TELESTO_YMIR_API TelestoYmirResult telesto_ymir_load_state(
    TelestoYmirContext *ctx,
    const char *path_utf8) {
    if (!ctx || !path_utf8) {
        return TELESTO_YMIR_INVALID_ARGUMENT;
    }

    try {
        const auto path = FromUtf8Path(path_utf8);
        if (!std::filesystem::exists(path)) {
            return ctx->Fail(TELESTO_YMIR_FILE_NOT_FOUND, "Save state file not found");
        }

        std::ifstream in{path, std::ios::binary};
        if (!in) {
            return ctx->Fail(TELESTO_YMIR_CORE_ERROR, "Could not open save state file for reading");
        }

        ymir::savestate::SaveState state{};
        cereal::PortableBinaryInputArchive archive{in};
        archive(state);

        if (!ctx->saturn.LoadState(state)) {
            return ctx->Fail(TELESTO_YMIR_CORE_ERROR, "Save state did not match the loaded Saturn session");
        }

        return TELESTO_YMIR_OK;
    } catch (const std::exception &ex) {
        return ctx->Fail(TELESTO_YMIR_CORE_ERROR, ex.what());
    }
}

TELESTO_YMIR_API void telesto_ymir_reset(TelestoYmirContext *ctx, int hard) {
    if (ctx) {
        ctx->saturn.Reset(hard != 0);
    }
}

TELESTO_YMIR_API void telesto_ymir_run_frame(TelestoYmirContext *ctx) {
    if (ctx) {
        ctx->saturn.RunFrame();
    }
}

TELESTO_YMIR_API void telesto_ymir_set_control_pad_state(
    TelestoYmirContext *ctx,
    uint32_t port,
    uint16_t pressed_buttons) {
    if (!ctx || port >= ctx->pressedButtons.size()) {
        return;
    }
    ctx->pressedButtons[port] = pressed_buttons;
}

} // extern "C"
