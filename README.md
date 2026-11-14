# Framework Sequencer

A reusable sequencing framework for Unity. Tell a guided flow, such as a tutorial, as an ordered list of steps. Steps finish through events, and the whole sequence can be skipped, rewound, paused, restarted or cancelled with clean-up guaranteed.

## Layers

| Assembly | Depends on | Purpose |
|---|---|---|
| `Framework.Sequencing.Core` | nothing, no engine references | `SequenceRunner`, steps, builder. Time is supplied by the caller. |
| `Framework.Sequencing.Unity` | Core, Tasks, Audio | `SequentialTask` runner component, `TaskStep`, signal and collider conditions |
| `Framework.Sequencing.Xr` | Core, XR Interaction Toolkit 3 | `InteractableCondition`. Only compiles when XRI is installed. |
| `Framework.Tasks` | Audio, CheckPoints, Outline | Scene-authored `Task` components |
| `Framework.Outline`, `Framework.Audio`, `Framework.CheckPoints`, `Framework.Tools` | | Supporting tools |

## Tell a sequence in code

```csharp
var tutorial = Sequence.Named("Controllers tutorial")
    .Do("Show welcome hint", ShowWelcomeHint)
    .Wait(2f)
    .WaitFor("Grab the paddle", paddleGrabbed)
    .Together("Both hands ready", leftHandReady, rightHandReady)
    .Do("Celebrate", PlayCelebration)
    .Build();

sequentialTask.Play(tutorial);
```

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

## Steps that wait for the player

A condition implements `IStepCondition`: it is armed when the step starts and disarmed when the step ends, whatever the reason.

- `SignalCondition`: call `Raise()` from any `UnityEvent`. Hand pose events and button events plug in here.
- `ColliderContactCondition`: satisfied when an accepted layer touches this collider, for example the ball hitting a paddle.
- `InteractableCondition` (XRI 3): satisfied on hover, select or activate of an interactable, for example poking a button or grabbing a paddle.

## Scene authoring

Add a `SequentialTask` to a GameObject, assign an `AudioSource` and list `Task` components. `ActionTask` combines narration, reminders, sub tasks and check points. `WaitForConditionTask` waits for any condition component. Call `Run()`, then use `Pause`, `Resume`, `SkipCurrentTask`, `GoBack`, `Restart` and `Cancel`.

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

Call `Finish()` from any event handler to complete the step. `OnExit` runs on every exit path (completed, skipped, rewound, restarted, cancelled), so clean up there.

## Installation

Add `https://github.com/BrusleyM/Framework-package.git#develop` in Package Manager. Requires Unity 2022.3 or newer and the Universal Render Pipeline.

## Tests

`Tests/Editor` holds EditMode tests for the core and the task adapter. `Tests/PlayMode` walks the Test scene sample through skip, pause, back, restart and cancel. Add the package to `testables` or keep it under `Assets` to run them.

## Known limitations

- XR adapters other than `InteractableCondition` are not provided. Wire Meta hand pose events to a `SignalCondition`.
- The outline shaders are URP only.
- `ActionTask` waits for feedback audio to finish by checking the audio source each frame.
