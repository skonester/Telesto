#include "telesto_ymir_core.h"

#include <cstdio>
#include <cstring>

// Exercise the real DLL boundary without redistributing firmware or games.
int main() {
    for (int iteration = 0; iteration < 3; ++iteration) {
        auto *ctx = telesto_ymir_create();
        if (!ctx) {
            std::fputs("Could not create Ymir context\n", stderr);
            return 1;
        }
        const auto result = telesto_ymir_load_disc(ctx, "__telesto_missing_disc__.cue");
        if (result != TELESTO_YMIR_FILE_NOT_FOUND ||
            std::strlen(telesto_ymir_last_error(ctx)) == 0) {
            std::fputs("Unexpected missing-disc result\n", stderr);
            return 1;
        }
        telesto_ymir_set_control_pad_state(ctx, 0, TELESTO_YMIR_BUTTON_START);
        telesto_ymir_reset(ctx, 1);
        telesto_ymir_destroy(ctx);
    }
    std::puts("Ymir DLL lifecycle smoke test passed");
    return 0;
}
