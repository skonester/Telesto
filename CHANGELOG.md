### Documentation

- Updated `AUTHORS.md` to credit the Telesto fork maintainer and preserve upstream Emutastic attribution.
- Updated `CONTRIBUTING.md` to clarify that this fork is focused on Sega Saturn emulation and Ymir integration.
- Added contributor guidance for Saturn/Ymir launch, BIOS, disc, save, input, and packaging work.

### Build

- Updated the native Ymir wrapper build to link the cereal and fmt dependencies needed for portable save-state serialization.
- Verified the native Ymir wrapper builds successfully.
- Verified the managed Telesto app builds successfully with `0` warnings and `0` errors.

