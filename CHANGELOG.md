# Changelog

## [Unreleased]

### Changed
- Breaking: the package is now `com.vishnu.framework.sequencer`. `Framework.CheckPoints`, `Framework.Outline` and `Framework.Tools` moved to the new `com.vishnu.framework.core` package, which this package depends on. Assembly names, namespaces and GUIDs are unchanged. Install both packages in the same project.

### Added
- README section "Build a sequence in the editor" describing the drag and drop authoring path.

### Changed
- `SequenceRunner.Advance` no longer allocates on a steady-state tick. Core tests now assert zero allocations and a time budget for a long running sequence.

## [2.0.0] - 2026-11-15

### Changed
- First stable release. It is 2.0.0-preview.3 with no code changes: the sequencer, stages, XR conditions and the developer inspector are now the supported 2.0 API.

## [2.0.0-preview.3] - 2026-11-15

### Added
- `SequentialTask` inspector for developers (editor only): state, previous, current and next stage and step, and controls for back, skip, restart, pause, resume and cancel.
- `SequenceRunner.Steps` and `StepWindow.Of(runner)`, which name the previous, current and next step.

## [2.0.0-preview.2] - 2026-11-15

### Added
- Stages: `StageStep` and `SequenceBuilder.Stage` group steps into a named stage with its own runner. Skipping, going back and restarting work per stage.
- `SequenceRunner.RestartCurrentStep`, which restarts the current step or stage.
- `TaskStage` and the `_stages` list on `SequentialTask` for authoring stages in a scene, with `SkipCurrentStage`, `GoBackStage`, `RestartCurrentStage`, `CurrentStageName` and `CurrentStageIndex`.
- Steps can report a failure while waiting for an event (`StepBase.Fail`), which faults the sequence, and receive `Pause` and `Resume` (`OnPause`, `OnResume`).

### Breaking
- `ISequenceStep` gained `Failed`, `Pause` and `Resume`. Steps derived from `StepBase` need no change; steps implementing the interface directly must add them.

## [2.0.0-preview.1] - 2026-11-15

### Breaking
| Old assembly | New assembly |
|---|---|
| `Framework` (root, held Outline and SnapObject) | `Framework.Tools` and `Framework.Outline` |
| `Audio` | `Framework.Audio` |
| `Task` | `Framework.Tasks` |
| `Sequential` | `Framework.Sequencing.Unity` |
| `Framework.Extensions` | removed |

Assembly references by name must be updated; references by GUID keep working except the removed `Framework.Extensions`.
`GameObjectExtension.SetLayerRecursively` and `RevertLayerRecursively` were removed.

### Fixed
- The package now declares the built-in modules it uses (animation, audio, physics); a project without them failed to compile.
- Restarting a sequence that had already finished started it without a clock, so timed steps never advanced.
- `EnableTask` now completes as soon as it has enabled its GameObject, so sub tasks listed after it start.

### Changed
- Skipping an `ActionTask` no longer plays its feedback audio.
- The README described `TaskManager` and `BaseTask`, which never existed in the code; it now documents the real types.
- Assemblies renamed under the `Framework.*` prefix and re-layered: `Framework.Audio`, `Framework.CheckPoints`, `Framework.Outline`, `Framework.Tasks`, `Framework.Tools`, `Framework.Sequencing.Unity`.
- `Framework.Outline` is now its own assembly instead of living in a catch-all root assembly.
- The manifest no longer advertises a Package Manager sample. The `Samples` folder holds the Test scene and ships with the package, but it is not a Package Manager sample.

### Changed (outline)
- Outline shaders are URP HLSL and SRP Batcher compatible; the package now depends on the Universal Render Pipeline.
- Outline no longer runs a per-frame coroutine, uses shared materials instead of instancing per toggle, raises `Disabled`, and resets its static mesh registry when entering play mode.

### Removed
- `GameObjectExtension` layer helpers (unused, held static state).
- Personal email from the package manifest.

### Added
- MIT license.
- Continuous integration and release workflows that test the package in a generated host project.
- `Framework.Sequencing.Core`: pure C# `SequenceRunner` with start, pause, resume, skip, back, restart and cancel, plus instant, delay, wait-for-condition, parallel and reminding steps and a fluent `Sequence` builder.
- `SequentialTask` now runs on the runner and exposes pause, resume, back, restart and cancel. `Run()` can start a finished sequence again and no longer clears the task list.
- Event-driven conditions: `SignalCondition`, `ColliderContactCondition`, `InteractableCondition`.
- EditMode and PlayMode tests.
