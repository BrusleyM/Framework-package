# Changelog

## [Unreleased]

### Added
- Sequence intro and complete audio, independent of the mode: `Intro Audio` plays once when the sequence is run (`Run()`, not `Restart()` or a mode switch), before the first task, and `Complete Audio` plays after the last task, so the sequence completes when it has finished. The intro is its own short run before the tasks, so a mode change during it lets it finish and the tasks then start in the new mode, unless `Allow Cutting Intro` is ticked, which stops it and starts the tasks at once. Both are lists of clips played one after another and are skipped when empty (no extra step is added). They are played by the new `AudioStep`, which pauses and resumes with the sequence. In the Test scene the Welcome audio task became the intro, and Task 3's feedback clip became the complete audio.
- Unguided mode. A `SequentialTask` has a Mode dropdown (`GuidanceMode`: Guided or Unguided) and uses the same task list for both. Guided runs the tasks one after another with their guidance, as before. Unguided starts every task at once, so they can be done in any order, with no guidance: nothing is highlighted or played, tasks that only give guidance (`AudioTask`) are left out, and a task's own feedback audio is not played (use the sequence's Complete Audio instead). It completes when every task is done. A hint is only shown when asked for: `RequestHint()` shows the hint of the first open task, or set Hint After Idle Seconds to hint by itself after that long without progress. The order the tasks were finished in is recorded (`CompletionOrder`) and reported as it happens (`UnguidedTaskCompleted` with the task and its 1-based position, `UnguidedCompleted` with whether it was the listed order). `SetMode()` (or the dropdown while playing) restarts a running sequence in the new mode on the next frame (a pause is kept; a sequence from `Play(runner)` is cancelled, not rebuilt), and `ModeChanged` reports it. Skipping the step runs the skipped tasks' complete events, the hint goes to the first open task that has one (`IHintable`), and `CompletionOrder` covers every stage. The logic is in the new `UnguidedStep`.
- Optional task behaviour for unguided mode and pausing, as capability interfaces (see Changed): `IUnguidedTask`, `IHintable`, `IPausableTask`, `ISettlingTask` and `IGuidanceOnlyTask`. `GuidedTask` implements the first four and runs setup subtasks when started unguided, plays the hint's instruction audio and runs the guidance subtasks on a hint, which are cancelled again when the task completes or the hint is hidden. A subtask has an Is Setup tick: setup subtasks (such as enabling the box to snap onto) run even when unguided, guidance subtasks only on a hint.
- Pausing a sequence now pauses the running task: `TaskStep` calls `PauseTask` and `ResumeTask` on tasks that implement `IPausableTask`. `AudioTask` holds its clip and does not move on to the next one while paused, and `ActionTask` pauses its audio and skips reminders. `AudioPlayer` gets `Pause()` and `Resume()`.

- The Test sample scene has no separate unguided section: set Mode on the `Tasks` object to Unguided and the same Task 1 to 3 run in any order (the Enable subtasks are marked as setup, so all three boxes are there from the start). The game manager forwards H to `RequestHint()`.

### Fixed
- In the Test sample scene the checkpoint boxes Cube (3) and Last (1) no longer have a Rigidbody, like First (1). With one, the block pushed the box (and its snap point) away before and during the snap, and a box overlapping a snapped block drifted off its place, so a snapped block ended up to 3 m from the authored checkpoint position. All three blocks now snap exactly onto their checkpoints.
- A long run of subtasks that finish instantly no longer grows the call stack: the action task starts the next listed subtask in a loop instead of from inside the previous one's completion event, so thousands of instant subtasks cannot overflow it. Empty slots are skipped in the same loop. A subtask that finishes during its own `Execute` now has the next one started after `Execute` returns, not from inside its completion event (a subtask that finishes later still starts the next one from its completion event), and cancelling or skipping the task while that subtask is finishing no longer lets the next one start.
- A parallel subtask that ends up containing itself, directly or through another group, logs an error and refuses a nested execute, and ignores a nested cancel, instead of recursing without end. The inspector already rejects such a slot; this covers lists set from code. A group's completed listeners now run after its children have finished executing, so a listener may execute the group again.

### Changed
- `Task` is back to its core lifecycle (start, complete, skip, cancel, prepare). The optional behaviour that was added to it as virtuals is now in capability interfaces in `Framework.Tasks`: `IUnguidedTask`, `IHintable`, `IPausableTask`, `ISettlingTask` and the marker `IGuidanceOnlyTask`. `GuidedTask` implements the first four and `AudioTask` implements `IPausableTask` and `IGuidanceOnlyTask`; their capability members are `virtual` so a subclass can override them. `TaskStep` and `UnguidedStep` check for the interface, so a task that implements none is started normally in unguided mode and is never hinted or paused. Also in this change: `Task.Prepare()` is virtual so `GuidedTask` resets its state when prepared again, and a hint is not shown for a task that is already complete. Breaking for custom tasks: `StartTaskUnguided`, `ShowHint`, `HideHint`, `PauseTask`, `ResumeTask`, `IsSettling`, `IsGuidanceOnly` and `HasHint` are no longer on `Task`, so drop `override` and implement the interface (`HasHint` is now simply implementing `IHintable`). A test guards the surface of `Task`.
- `GuidedTask` keeps its guidance in one explicit `State` (`GuidanceState`: Idle, Guided, Unguided, Hinting, Completed) instead of five boolean flags, so an invalid combination can no longer be expressed. The subtask ordering moved to the new `SubtaskSequencer` (with `SubtaskFilter`), a plain class that runs the subtasks one after another without nesting, can run only the setup ones, tear down all or only the guidance ones, and stop. Behaviour is unchanged except that a hint is now only accepted while the task is unguided (it was accepted but meaningless while guided), feedback audio only plays when the task was started guided (not when completed from idle), and `Task.Prepare()` is virtual so `GuidedTask` goes back to idle when prepared again after it completed.
- `ActionTask` is split. Its guidance (instruction and feedback audio, reminders, subtasks, setup versus guidance, hints, pause) is now in the abstract `GuidedTask` in `Framework.Tasks`, with three hooks for the completion: `BeginCompletionTracking`, `EndCompletionTracking` and `ResetCompletionTracking`. `ActionTask` is a `GuidedTask` that completes when its check points are all checked. `ActionTask`, `TrackCheckPoint` and the highlight tasks (`HighlightTask`, `SequentialHighlightTask`, `SimultaneousHighlightTask`, `EnableAndHighlightTask`) moved to the new assembly `Framework.Tasks.Interaction`, so `Framework.Tasks` no longer references `Framework.CheckPoints` or `Framework.Outline` and a task that completes another way (a quiz answer, a timer) can reuse the guidance. Scenes and prefabs are not affected (same classes, namespace and script GUIDs, and the serialized fields kept their names). Breaking for code: anything that uses these classes (including test and sample assemblies) must reference `Framework.Tasks.Interaction`. A test guards that `Framework.Tasks` stays free of the core references.
- The unguided hint after idle time uses `CountdownTimer` from `Framework.Timing` in `com.vishnu.framework.core` instead of counting seconds itself. `Framework.Sequencing.Unity` now references `Framework.Timing`, so the core package version that has it (the one after 1.1.0) is required. Behaviour is unchanged.
- The Test sample's `GameManager` no longer has its own `GameMode` enum and `SetGameMode(GameMode)`: the mode is on the `SequentialTask` (`GuidanceMode`), and `SetGameMode(GuidanceMode)` forwards to `SetMode`. Its hint button no longer skips the current task; it calls `RequestHint()`.
- While the intro or complete audio plays, `CurrentStageIndex` is -1.
- The Robot instructor (`MoveTo` in the Base Models sample) is confined to a home zone (`Zone Centre`, default where it starts, and `Zone Radius`, default 8) and no longer follows every head movement. It only repositions when it is outside the distance band (`Min Dist To Target` to `Max Dist To Target`, measured along the floor), when the user has turned so far that its spot in front of them is more than `Turn Threshold Degrees` (default 40) away from where it is, or when it is outside its zone, and only if the new spot is more than `Minimum Move Distance` away. The rules are in the pure `FollowRules` class, with edit mode tests.
- The Test sample scene's guide character now comes from `Robot instructor.prefab` and `Robot instructor.fbx` (the former Ligma files renamed in `Samples/Base Models/Mesh`: same mesh and materials, and the root object inside the prefab is still named `Ligma`). The prefab now includes what the scene used to override on it (position, rotation, emission, samples and the audio source) and drops a disabled duplicate component. The game manager is its own `gm` object under Managers instead of living on the character, still pointing at the sequence on the `Tasks` object.
- The Test sample scene uses `SnapPoint`, `Snappable` and `SnapKey` instead of the obsolete `SnapObject`: each checkpoint object accepts one key (Middle, First or Last, in `Samples/Scenes/Snap Keys`), each block carries the matching key, and a snapped block glides into place and ticks its checkpoint through the snap point's snapped event. Matching no longer depends on object names.
- The Test sample scene is grouped by role: Managers (the game manager), Tasks, Checkpoints, Blocks, Player and Environment, and every task that has subtasks keeps them in a `Subtasks` child. The `SequentialTask` component sits on the `Tasks` object instead of on the game manager object, and the game manager points at it. No object moved in the world, and the scene still starts its sequence on the welcome task.

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
