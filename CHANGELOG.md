# Changelog

## 0.2 - 2025-08-14

### Added

* Support for keeping multiple models initialized simultaneously.
* `AviadClusters` for configuring a classification text in Editor.
* Several new AI components for managing data for NPC dialogue.
* Option to set the logging level for the package.
* A DemoScene in Samples with animation and dialogue examples.

### Changed

* `AviadRunner` now enforces a strict execution order to help prevent buggy implementations.
* Updated llama.cpp configuration types to support nearly all possible settings.
* Move model configuration to AviadModel ScriptableObject.

### Fixed

* All callbacks from Task.run are now run on the main thread.
* Resolved bugs causing less efficient context reuse.
