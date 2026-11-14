# Changelog

## [2.0.0-preview.1] - Unreleased

### Changed
- Assemblies renamed under the `Framework.*` prefix and re-layered: `Framework.Audio`, `Framework.CheckPoints`, `Framework.Outline`, `Framework.Tasks`, `Framework.Tools`, `Framework.Sequencing.Unity`.
- `Framework.Outline` is now its own assembly instead of living in a catch-all root assembly.
- Package manifest sample path now points at the real `Samples` folder.

### Changed (outline)
- Outline shaders are URP HLSL and SRP Batcher compatible; the package now depends on the Universal Render Pipeline.
- Outline no longer runs a per-frame coroutine, uses shared materials instead of instancing per toggle, raises `Disabled`, and resets its static mesh registry when entering play mode.

### Removed
- `GameObjectExtension` layer helpers (unused, held static state).
- Personal email from the package manifest.

### Added
- MIT license.
- `Framework.Sequencing.Core`: pure C# `SequenceRunner` with start, pause, resume, skip, back, restart and cancel, plus instant, delay, wait-for-condition, parallel and reminding steps and a fluent `Sequence` builder.
- `SequentialTask` now runs on the runner and exposes pause, resume, back, restart and cancel.
- Event-driven conditions: `SignalCondition`, `ColliderContactCondition`, `InteractableCondition`.
- EditMode and PlayMode tests.
