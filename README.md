# Framework Sequencer

A reusable sequencing framework for Unity. Tell a guided flow, such as a tutorial, as an ordered list of steps. Steps finish through events, and the whole sequence can be skipped, rewound, paused, restarted or cancelled with clean-up guaranteed.

## Layers

| Assembly | Depends on | Purpose |
|---|---|---|
| `Framework.Sequencing.Core` | nothing, no engine references | `SequenceRunner`, steps, builder. Time is supplied by the caller. |
| `Framework.Sequencing.Unity` | Core, Tasks, Audio, `Framework.Timing` from the core package | `SequentialTask` runner component, `TaskStep`, signal and collider conditions, `WaitForConditionTask` |
| `Framework.Sequencing.Unity.Editor` | Core, Unity | Editor only. The `SequentialTask` inspector: previous, current and next stage and step, and developer controls. |
| `Framework.Sequencing.Xr` | Core, XR Interaction Toolkit 3.x | `InteractableCondition`. Compiles only when XRI 3.x is installed (version define `FRAMEWORK_XR_INTERACTION_TOOLKIT`). |
| `Framework.Tasks` | Audio | Scene-authored `Task` components that need nothing from the core package: `Task`, `GuidedTask` (audio, reminders, subtasks, hints, pause, with the completion left to the subclass), `AudioTask`, subtasks. Derive from `GuidedTask` for a task that completes some other way, such as a quiz answer. |
| `Framework.Tasks.Interaction` | Tasks, `Framework.CheckPoints` and `Framework.Outline` from `com.vishnu.framework.core` | The tasks that use check points and outlines: `ActionTask` (a `GuidedTask` that completes when all its check points are checked), `TrackCheckPoint` and the highlight subtasks. |
| `Framework.Audio` | | Narration playback for tasks. |

Requires Unity 2022.3 or newer and the Universal Render Pipeline.

## Tell a sequence in code

```csharp
var tutorial = Sequence.Named("Controllers tutorial")
    .Do("Show welcome hint", ShowWelcomeHint)
    .Wait(2f)
    .WaitFor("Grab the paddle", paddleGrabbed)
    .Together("Both hands ready",
        new WaitForConditionStep("Left hand", leftHandReady),
        new WaitForConditionStep("Right hand", rightHandReady))
    .Then(new RemindingStep(new WaitForConditionStep("Hit the ball", ballHit), 8f, PlayReminder))
    .Do("Celebrate", PlayCelebration)
    .Build();

sequentialTask.Play(tutorial);
```

`Then(ISequenceStep)` adds any step, including your own. The built-in steps are `InstantStep`, `DelayStep`, `WaitForConditionStep`, `ParallelStep` and `RemindingStep`.

`SequenceRunner` is plain C#. Drive it yourself with `Advance(deltaSeconds)` in tests or in any engine loop.

```csharp
tutorial.Start();
tutorial.Advance(0.016f);
tutorial.SkipCurrentStep();
tutorial.GoBack();
tutorial.Pause();
tutorial.Resume();
tutorial.Restart();
tutorial.Cancel();
```

Events: `StepEntered`, `StepExited`, `StateChanged`, `Completed`, `Cancelled`, `Faulted`. `State` is one of `Idle`, `Running`, `Paused`, `Completed`, `Cancelled`, `Faulted`.

Behaviour worth knowing:
- Commands issued from event handlers or step callbacks are queued and run after the current transition.
- A step that throws faults the sequence. The step is exited as `Cancelled` and `Faulted` carries the exception. `SequentialTask` logs it.
- `GoBack` on the first step re-enters the first step.
- `Skip` and `GoBack` while paused move to the other step but stay paused.
- `Restart` works from any state and resumes a paused sequence.
- An exception thrown by an event handler is rethrown to the caller once the sequence is consistent.

## Stages

A stage is a named group of steps with its own runner inside the sequence. Skipping, going back and restarting then work on a whole stage as well as on a single step.

```csharp
var tutorial = Sequence.Named("Tutorial")
    .Stage("Intro", intro => intro
        .Do("Show welcome", ShowWelcome)
        .WaitFor("Grab the paddle", paddleGrabbed))
    .Stage("Hands", hands => hands
        .WaitFor("Point at the button", pointed)
        .WaitFor("Pinch", pinched))
    .Build();

tutorial.SkipCurrentStep();
tutorial.RestartCurrentStep();
tutorial.GoBack();
```

On the outer runner, `SkipCurrentStep` skips the whole running stage (its current step is cleaned up as cancelled), `RestartCurrentStep` begins the current stage again from its first step, and `GoBack` returns to the previous stage from its first step. The runner inside a stage is `StageStep.Stage`, and moves one step at a time. Pausing the sequence pauses the running stage, and a failure inside a stage faults the whole sequence.

In a scene, add a `TaskStage` component per stage, list its tasks, and list the stages on `SequentialTask`. When any stage is listed they replace the plain task list. `SkipCurrentTask` and `GoBack` move within the current stage, and `SkipCurrentStage`, `GoBackStage` and `RestartCurrentStage` move between stages. `CurrentStageName` and `CurrentStageIndex` say where the player is.

## Steps that wait for the player

A condition implements `IStepCondition`: it is armed when the step starts and disarmed when the step ends, whatever the reason. A condition may be satisfied synchronously while arming.

- `SignalCondition`: call `Raise()` from any `UnityEvent`. Hand pose events and button events plug in here. Signals raised while it is not armed are ignored, not remembered.
- `ColliderContactCondition`: satisfied when a collider on an accepted layer touches this one, including contact that already exists. It accepts no layers until you choose them.
- `InteractableCondition` (XRI 3.x): satisfied on hover or select, or on activate, of an interactable. It also completes at once if the interactable is already hovered or selected. Whether activate fires depends on your interaction profile, so select is the reliable choice for pokes and grabs.

## Unguided mode

A `SequentialTask` has a Mode dropdown, Guided or Unguided, and uses the same task list for both. Guided walks the user through the tasks one at a time. Unguided starts every task at once, so the user can do them in any order, with no audio, reminders or highlights, and the sequence completes when every task is done.

A task's check points never force an order: an `ActionTask` completes when all its check points are checked, in any order. What changes in unguided mode is how the tasks are scheduled and what guidance runs:

- A subtask has an Is Setup tick. Setup subtasks (such as enabling the box a block snaps onto) run in both modes. Guidance subtasks (highlights) run in guided mode, and in unguided mode only when a hint is asked for.
- A hint is only shown when asked for: call `RequestHint()` on the sequence (for example from a button or a UnityEvent), or set Hint After Idle Seconds to hint by itself after that long without progress. It shows the hint of the first open task and is taken down when that task finishes.
- Tasks that only give guidance, such as `AudioTask`, are skipped in unguided mode.
- The order the tasks were finished in is available as `CompletionOrder` and reported as it happens (`UnguidedTaskCompleted` with the task and its 1-based position, `UnguidedCompleted` with whether it matched the list order). That is useful when order matters. The tasks' own feedback audio is not played in unguided mode.
- Changing the mode while playing (the dropdown or `SetMode`) restarts the sequence in the new mode on the next frame, keeping a pause. A sequence started with `Play(runner)` is cancelled, not rebuilt. `SkipCurrentTask` in unguided mode skips the whole unguided step (every open task, running their complete events), and skipping a task reverts what its subtasks changed, including the setup ones.
- A hint goes to the first open task that has one (a task that implements `IHintable`; an `ActionTask` does).

`Intro Audio` and `Complete Audio` on the `SequentialTask` play in both modes: the intro once when the sequence is run (`Run()` plays it; `Restart()` and a mode switch do not), the complete audio after the last task. The intro is its own short run before the tasks start, so the mode that applies is the one set when it ends. Changing the mode during the intro lets it finish by default, and cuts it and starts the tasks at once when Allow Cutting Intro is ticked. While they play, `CurrentStageIndex` is -1. Use them instead of an audio task at the start and a feedback clip on the last task. A custom task opts in by implementing the capability interfaces it needs: `IUnguidedTask` (start without guidance), `IHintable` (show and hide a hint), `IPausableTask`, `ISettlingTask` (still playing its own audio after completing: `TaskStep` waits for it before moving on) and the marker `IGuidanceOnlyTask` (skipped in unguided mode). `Task` itself only has start, complete, skip, cancel and prepare; a task that implements none of them is started normally in unguided mode and is never hinted or paused. A subclass of `GuidedTask` or `AudioTask` customises these by overriding their virtual members (`ShowHint`, `HideHint`, `StartTaskUnguided`, `PauseTask`, `ResumeTask`, `IsSettling`).

## Build a sequence in the editor

No code is needed to author a sequence. Everything is a component you add and wire in the Inspector.

1. Add a `SequentialTask` to a GameObject and assign an `AudioSource` for narration.
2. Create a child GameObject for each step and add a `Task` component to it: `AudioTask` plays a clip, `ActionTask` combines narration, reminders, sub tasks and check points (sub tasks run one after another; put them in a `ParallelSubtask` to run them at the same time), and `WaitForConditionTask` waits for any condition component such as `SignalCondition` or `ColliderContactCondition`.
3. Drag the task objects into the `Task List` field of the `SequentialTask`, in the order they should run. Reorder them by dragging the handle beside each element.
4. For a sequence in named stages, add a `TaskStage` component to a GameObject, set its name and list its tasks, then drag the stage objects into the `Stages` field. When `Stages` is not empty, the stages run and the `Task List` is ignored.
5. Call `Run()` from a `UnityEvent` (a button, a trigger or a hand pose event) or from code.

`Run()` does nothing while a sequence is in progress, and starts over once it has ended. `Play(runner)` cancels the current runner first. `Pause`, `Resume`, `SkipCurrentTask`, `GoBack`, `Restart` and `Cancel` act on the running sequence. The component must be active and enabled to run.

The Test scene at `Samples/Scenes/Test scene.unity` is built this way. Open it and select the object that holds the `SequentialTask` to see a working task list.

## Developer inspector

In play mode the `SequentialTask` inspector shows the sequence state, the previous, current and next stage and step, and buttons for Back, Skip and Restart (per stage), Pause, Resume, Restart and Cancel. It updates on step and state events. It is editor only and adds nothing to builds. `StepWindow.Of(runner)` gives the same previous, current and next names to your own tools.

The Test scene lives at `Samples/Scenes/Test scene.unity`. The `Samples` folder is imported and compiled with the package; it is not offered as a Package Manager sample.

## Write your own step

```csharp
public sealed class ShowHintStep : StepBase
{
    private readonly GameObject _hint;

    public ShowHintStep(GameObject hint) : base("Show hint") { _hint = hint; }

    protected override void OnEnter() => _hint.SetActive(true);

    protected override void OnExit(ExitReason reason) => _hint.SetActive(false);
}
```

Call `Finish()` from any event handler to complete the step, or `Fail(exception)` to fault the sequence. Override `OnPause` and `OnResume` if the step has its own timers or audio to hold. `OnExit` runs on every exit path (completed, skipped, rewound, restarted, cancelled), so clean up there.

## Installation

Install core first, then the sequencer, both by git URL in Package Manager. Unity does not resolve git-URL dependencies, so installing the sequencer alone fails with `Package [com.vishnu.framework.core@1.0.0] cannot be found`.

1. `https://github.com/BrusleyM/Framework-core.git#v1.0.0`
2. `https://github.com/BrusleyM/Framework-package.git#v3.0.1`

## Tests

`Tests/Editor` holds EditMode tests for the core, the task adapter and the conditions. `Tests/PlayMode` runs `SequentialTask` in play mode with tasks built in code. Keep the package under `Assets`, or add it to `testables`, to run them.

## Continuous integration and releases

Everything runs in this repository's own GitHub Actions and depends on no consuming project.

| Workflow | When | What |
|---|---|---|
| `ci.yml` | pull requests into `develop` or `master`, pushes to `develop`, by hand | lints the workflows, checks the manifest, `.meta` files, assembly references and changelog, runs the core tests without Unity, then runs the EditMode and play mode tests in a generated host project on Unity 2022.3, Unity 6000.3, and 6000.3 with XR Interaction Toolkit |
| `release-prepare.yml` | by hand on `develop` | bumps `package.json`, moves the changelog's Unreleased section under the new version, and opens a pull request into `develop` |
| `release-promote.yml` | push to `develop`, by hand | when `develop` holds a prepared version that has no tag yet, opens a pull request from `develop` into `master` |
| `release-publish.yml` | push to `master` | tags `vX.Y.Z` and creates the GitHub release from the changelog section |

Changes move `feature` to `develop` to `master`. To release: run `release-prepare`, merge its pull request into `develop`, then merge the pull request that `release-promote` opens into `master`. `develop` already holds the version and changelog, so no back-merge is needed.

The version lives only in `package.json`. Choose the bump when you run `release-prepare`: `current` releases the version already written, `stable` drops a `-preview.N` label, `patch`, `minor` and `major` bump that number and reset the lower ones, and `preview` continues `-preview.N`. Pre-releases are published as GitHub pre-releases.

Set up once:
- Secrets `UNITY_EMAIL` and `UNITY_PASSWORD` (a Unity Personal account) for the Unity test jobs.
- Repository setting Actions > General > Allow GitHub Actions to create and approve pull requests.
- Branch protection on `develop` and `master` requiring the CI jobs, so a release cannot merge with failing tests.

To run the checks locally: `python .github/scripts/check_package.py`, `python -m unittest test_release_tools` from `.github/scripts`, and `dotnet test .github/core-tests`.

## Known limitations

- Only `InteractableCondition` is provided for XR. Wire Meta hand pose events to a `SignalCondition` for gestures.
- The outline shaders are URP only and rely on a stencil capable depth buffer. They have not been checked on a Quest.
- `OutlineWidth` is a view-space thickness, so the visible width varies with field of view.
- `ColliderContactCondition` is one shot and will miss a ball resting asleep on the collider when armed.
- A task that is still settling, for example `ActionTask` playing feedback audio, is checked on each runner tick until it settles.
- A step instance holds its own state. Build a new sequence for each run instead of reusing step instances across runners.
