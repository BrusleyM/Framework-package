using System;
using System.Collections.Generic;

namespace Framework.Sequencing
{
    public sealed class SequenceRunner
    {
        private readonly ISequenceStep[] _steps;
        private ISequenceStep _activeStep;
        private bool _isEnteringStep;
        private bool _activeStepFinishedWhileEntering;
        private bool _activeStepFinishedWhilePaused;

        public SequenceRunner(string name, IEnumerable<ISequenceStep> steps)
        {
            Name = name;
            _steps = new List<ISequenceStep>(steps).ToArray();
        }

        public string Name { get; }
        public SequenceState State { get; private set; } = SequenceState.Idle;
        public int CurrentIndex { get; private set; } = -1;
        public int StepCount => _steps.Length;
        public ISequenceStep CurrentStep => _activeStep;
        public bool IsInProgress => State == SequenceState.Running || State == SequenceState.Paused;

        public event Action<ISequenceStep, int> StepEntered;
        public event Action<ISequenceStep, int, ExitReason> StepExited;
        public event Action<SequenceState> StateChanged;
        public event Action Completed;
        public event Action Cancelled;
        public event Action<Exception> Faulted;

        public void Start()
        {
            if (State != SequenceState.Idle)
            {
                return;
            }

            BeginFromFirstStep();
        }

        public void Restart()
        {
            LeaveActiveStep(ExitReason.Restarted);
            BeginFromFirstStep();
        }

        public void Advance(float deltaSeconds)
        {
            if (State != SequenceState.Running || _activeStep == null)
            {
                return;
            }

            var step = _activeStep;
            Guard(() => step.Advance(deltaSeconds));
        }

        public void Pause()
        {
            if (State == SequenceState.Running)
            {
                ChangeState(SequenceState.Paused);
            }
        }

        public void Resume()
        {
            if (State != SequenceState.Paused)
            {
                return;
            }

            ChangeState(SequenceState.Running);

            if (_activeStepFinishedWhilePaused)
            {
                _activeStepFinishedWhilePaused = false;
                MoveToNextStep(ExitReason.Completed);
            }
        }

        public void SkipCurrentStep()
        {
            if (IsInProgress && _activeStep != null)
            {
                MoveToNextStep(ExitReason.Skipped);
            }
        }

        public void GoBack()
        {
            if (!IsInProgress || _activeStep == null)
            {
                return;
            }

            var previousIndex = Math.Max(0, CurrentIndex - 1);
            LeaveActiveStep(ExitReason.Rewound);
            EnterStep(previousIndex);
        }

        public void Cancel()
        {
            if (!IsInProgress)
            {
                return;
            }

            LeaveActiveStep(ExitReason.Cancelled);
            ChangeState(SequenceState.Cancelled);
            Cancelled?.Invoke();
        }

        private void BeginFromFirstStep()
        {
            ChangeState(SequenceState.Running);

            if (_steps.Length == 0)
            {
                FinishSequence();
                return;
            }

            EnterStep(0);
        }

        private void EnterStep(int index)
        {
            var step = _steps[index];
            CurrentIndex = index;
            _activeStep = step;
            _activeStepFinishedWhileEntering = false;
            _activeStepFinishedWhilePaused = false;
            step.Finished += OnActiveStepFinished;

            StepEntered?.Invoke(step, index);

            _isEnteringStep = true;
            Guard(step.Enter);
            _isEnteringStep = false;

            if (State == SequenceState.Faulted || _activeStep != step)
            {
                return;
            }

            if (_activeStepFinishedWhileEntering)
            {
                OnActiveStepFinished();
            }
        }

        private void OnActiveStepFinished()
        {
            if (_isEnteringStep)
            {
                _activeStepFinishedWhileEntering = true;
                return;
            }

            if (State == SequenceState.Paused)
            {
                _activeStepFinishedWhilePaused = true;
                return;
            }

            if (State == SequenceState.Running)
            {
                MoveToNextStep(ExitReason.Completed);
            }
        }

        private void MoveToNextStep(ExitReason reason)
        {
            var nextIndex = CurrentIndex + 1;
            LeaveActiveStep(reason);

            if (State == SequenceState.Faulted)
            {
                return;
            }

            if (nextIndex >= _steps.Length)
            {
                FinishSequence();
                return;
            }

            EnterStep(nextIndex);
        }

        private void LeaveActiveStep(ExitReason reason)
        {
            var step = _activeStep;
            if (step == null)
            {
                return;
            }

            _activeStep = null;
            step.Finished -= OnActiveStepFinished;
            Guard(() => step.Exit(reason));
            StepExited?.Invoke(step, CurrentIndex, reason);
        }

        private void FinishSequence()
        {
            ChangeState(SequenceState.Completed);
            Completed?.Invoke();
        }

        private void ChangeState(SequenceState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }

        private void Guard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Fault(exception);
            }
        }

        private void Fault(Exception exception)
        {
            if (State == SequenceState.Faulted)
            {
                return;
            }

            var step = _activeStep;
            _activeStep = null;

            if (step != null)
            {
                step.Finished -= OnActiveStepFinished;
                try
                {
                    step.Exit(ExitReason.Cancelled);
                }
                catch (Exception)
                {
                }
            }

            ChangeState(SequenceState.Faulted);
            Faulted?.Invoke(exception);
        }
    }
}
