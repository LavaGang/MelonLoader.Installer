## What's Changed
* Added a search bar for the game library
* Added proper handling for games located in directories which require elevated privileges to write to. The installer will now ask the user whether they'd like to restart with elevated privileges.
* Added support for Meta Store games
* Added a fallback method for detecting the target architecture of games
* Fixed a case where the installer would show negative CI build numbers on MelonLoader versions
* Fixed icons for Steam games
* Fixed the Linux launch guide for Wine
* Updated third-party dependencies. This might fix some of the existing launching issues.

## New Contributors
* @psyellas made their first contribution in https://github.com/LavaGang/MelonLoader.Installer/pull/48
* @nalka0 made their first contribution in https://github.com/LavaGang/MelonLoader.Installer/pull/41
* @winterheart made their first contribution in https://github.com/LavaGang/MelonLoader.Installer/pull/39

**Full Changelog**: https://github.com/LavaGang/MelonLoader.Installer/compare/4.1.1...v4.1.2