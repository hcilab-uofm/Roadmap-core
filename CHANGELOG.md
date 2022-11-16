# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.1] - 2022-11-16
### Added
- Calculated and added transformation between VR and AR

### Changed
- Fix - Wrong coordinate space from x,y,z to lat, long, alt

## [0.2.0] - 2022-11-07
### Added
- All placeable objects are MRTK-Interactables
- Adding firebase integration
- Add additional interface in VR
  - Sync local and remote with appropriate confirmations
  - Remove/Modify buttons
- Placeables data has unique ID's and timestamps to allow sync
- Editor UI to set group ID
- Editor Menu option to generate default files
- MRTK patch (following https://github.com/microsoft/MixedRealityToolkit-Unity/issues/10449#issuecomment-1111163353 )
- Editor Menu option to apply MRTK patch

### Changed
- Fixing MRTK handles of objects with propper configs
- Refactor internal methods
  - Modify button callback
  - BoundsControl & TapToPlace callbeck
- Making Menu UI in AR scene a prefab

## [0.1.1] - 2022-10-20
### Added
- UI for `RoadmapApplicationConfig`
- UI for deploying and building applications
- Menu option to setup URP

### Changed
- Fix one of many spelling mistakes: Oculs -> Oculus
- MRTK assets are configured internally

### Removed
- MRTK assets are now hidden in inspector

## [0.1.0] - 2022-10-18
### Added
- Intial implementation
  - VR and AR components with basic functionalies.
