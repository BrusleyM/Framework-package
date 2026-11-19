# Changelog

## [Unreleased]

### Changed
- The Test sample scene is grouped by role: Managers (the game manager), Tasks, Checkpoints, Blocks, Player and Environment, and every task that has subtasks keeps them in a `Subtasks` child. The `SequentialTask` component sits on the `Tasks` object instead of on the game manager object, and the game manager points at it. No object moved in the world, and the scene still starts its sequence on the welcome task.

### Fixed
- A long run of subtasks that finish instantly no longer grows the call stack: the action task starts the next listed subtask in a loop instead of from inside the previous one's completion event, so thousands of instant subtasks cannot overflow it. Empty slots are skipped in the same loop. A subtask that finishes during its own `Execute` now has the next one started after `Execute` returns, not from inside its completion event (a subtask that finishes later still starts the next one from its completion event), and cancelling or skipping the task while that subtask is finishing no longer lets the next one start.
- A parallel subtask that ends up containing itself, directly or through another group, logs an error and refuses a nested execute, and ignores a nested cancel, instead of recursing without end. The inspector already rejects such a slot; this covers lists set from code. A group's completed listeners now run after its children have finished executing, so a listener may execute the group again.

## [3.1.0] - 2026-11-18

### Added
- `EnableTask` and `SimultaneousHighlightTask` have a state field: On (default, same behavior as before), Off, or Toggle. A toggle flips from whatever the object currently is (on a highlight task, each object flips on its own), so prefer On or Off unless a flip is what you want.
- `ParallelSubtask`: a subtask that holds a list of other subtasks, starts them all at once and completes when every one of them has completed. A slot that would make a group contain itself, directly or through another group, is cleared with an error. Put it in an action task's subtask list to run, for example, a highlight and an enable together while the rest of the list still runs in order.

### Changed
- `SimultaneousHighlightTask.Cancel` puts each outline back to the state it had before the task ran (nothing happens if the task never ran), the same as `EnableTask.Cancel`.
- `EnableTask.Cancel` puts its game object back to the state it had before the task ran (nothing happens if the task never ran), so subtasks nested in a `ParallelSubtask` are reset the same way as listed ones, whatever their state. The action task no longer special-cases `EnableTask` when it tears down.
- The action task and `ParallelSubtask` cancel their subtasks from the last listed to the first, so an object touched by more than one subtask is restored correctly.

### Deprecated
- `EnableTask.DisableGameObject`: use `Cancel`, which restores the state the object had before the task ran instead of forcing it off, and does not complete the task. `DisableGameObject` still switches the object off and completes the task, and will be removed in the next major release.

## [3.0.1] - 2026-11-18

### Changed
- Depends on the stable `com.vishnu.framework.core` 1.0.0 instead of 1.0.0-preview.1. Install core 1.0.0 before this package.

## [3.0.0] - 2026-11-18

### Changed
- Breaking: the package is now `com.vishnu.framework.sequencer`. `Framework.CheckPoints`, `Framework.Outline` and `Framework.Tools` moved to the new `com.vishnu.framework.core` package, which this package depends on. Assembly names, namespaces and GUIDs are unchanged. Install both packages in the same project.

### Added
- README section "Build a sequence in the editor" describing the drag and drop authoring path.

### Fixed
- Imported samples no longer warn that `EmissionControl.cs` and `MoveTo.cs` do not belong to an assembly definition. They now compile in `Framework.Samples.BaseModels`.

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
