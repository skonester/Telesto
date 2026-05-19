### Documentation

- Updated `AUTHORS.md` to credit the Telesto fork maintainer and preserve upstream Emutastic attribution.
- Updated `CONTRIBUTING.md` to clarify that this fork is focused on Sega Saturn emulation and Ymir integration.
- Added contributor guidance for Saturn/Ymir launch, BIOS, disc, save, input, and packaging work.

### Stability

- Reverted the embedded Ymir runtime back to the last known working baseline from commit `1f566b5`.
- Fixed a regression where Saturn games could close or stop progressing after the initial Sega boot screen.
- Crash logs showed the BIOS, IPL, and disc were loading successfully before the process terminated, which pointed to the post-load embedded Ymir/native runtime path rather than bad game images or BIOS discovery.
- Windows crash reports showed access-violation/native runtime failures (`0xc0000005` and CoreCLR `0x80131506`) after the newer embedded Ymir feature batch was added.
- Deferred the unstable post-`1f566b5` Ymir changes so they can be rebuilt incrementally with boot smoke tests after each step.

### Deferred Ymir Work

- Save-state exports and UI wiring were reverted for this release.
- Experimental native wrapper serialization dependencies were reverted.
- Pause/reset toolbar work and launch-with-save-state routing were deferred.
- Future embedded Ymir features should be added behind narrow feature gates, starting from the restored `1f566b5` baseline.

### Build

- Rebuilt the native Ymir wrapper successfully after restoring the stable baseline.
- Rebuilt the managed Telesto app successfully with `0` warnings and `0` errors.
- Published a standalone Windows x64 build after the revert.
- Restored standalone Ymir SDL3 discovery in core preferences for both `ymir.sdl3.exe` and `ymir-sdl3.exe` payload names.
