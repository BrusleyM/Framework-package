using System;
using System.Collections.Generic;

namespace Framework.Sequencing
{
    public sealed class SequenceRunner
    {
        private readonly ISequenceStep[] _steps;
        private readonly Queue<Action> _queuedCommands = new Queue<Action>();
        private readonly List<Exception> _handlerFailures = new List<Exception>();
        private ISequenceStep _activeStep;
        private bool _isTransitioning;
        private bool _activeStepFinishedWhilePaused;

        public SequenceRunner(string name, IEnumerable<ISequenceStep> steps)
        {
            Name = name;
            _steps = new List<ISequenceStep>(steps).ToArray();

            foreach (var step in _steps)
            {
                if (step == null)
                {
                    throw new ArgumentException("A sequence cannot contain a null step.", nameof(steps));
                }
            }
        }

        public string Name { get; }
        public SequenceState State { get; private set; } = SequenceState.Idle;
        public int CurrentIndex { get; private set; } = -1;
        public int StepCount => _steps.Length;
        public IReadOnlyList<ISequenceStep> Steps => _steps;
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
            Execute(() =>
            {
                if (State == SequenceState.Idle)
                {
                    BeginFromFirstStep();
                }
            });
        }

        public void Restart()
        {
            Execute(() =>
            {
                LeaveActiveStep(ExitReason.Restarted);

                if (State != SequenceState.Faulted)
                {
                    BeginFromFirstStep();
                }
            });
        }

        public void Advance(float deltaSeconds)
        {
            if (_isTransitioning || State != SequenceState.Running || _activeStep == null)
            {
                return;
            }

            var step = _activeStep;
            Execute(() => Guard(() => step.Advance(deltaSeconds)));
        }

        public void Pause()
        {
            Execute(() =>
            {
                if (State != SequenceState.Running)
                {
                    return;
                }

                ChangeState(SequenceState.Paused);
                var step = _activeStep;
                if (step != null)
                {
                    Guard(step.Pause);
                }
            });
        }

        public void Resume()
        {
            Execute(() =>
            {
                if (State != SequenceState.Paused)
                {
                    return;
                }

                ChangeState(SequenceState.Running);
                var step = _activeStep;
                if (step != null)
                {
                    Guard(step.Resume);
                }

                if (_activeStepFinishedWhilePaused)
                {
                    _activeStepFinishedWhilePaused = false;
                    MoveToNextStep(ExitReason.Completed);
                }
            });
        }

        public void SkipCurrentStep()
        {
            Execute(() =>
            {
                if (IsInProgress && _activeStep != null)
                {
                    MoveToNextStep(ExitReason.Skipped);
                }
            });
        }

        public void RestartCurrentStep()
        {
            Execute(() =>
            {
                if (!IsInProgress || _activeStep == null)
                {
                    return;
                }

                var index = CurrentIndex;
                LeaveActiveStep(ExitReason.Restarted);

                if (State != SequenceState.Faulted)
                {
                    EnterStep(index);
                }
            });
        }

        public void GoBack()
        {
            Execute(() =>
            {
                if (!IsInProgress || _activeStep == null)
                {
                    return;
                }

                var previousIndex = Math.Max(0, CurrentIndex - 1);
                LeaveActiveStep(ExitReason.Rewound);

                if (State != SequenceState.Faulted)
                {
                    EnterStep(previousIndex);
                }
            });
        }

        public void Cancel()
        {
            Execute(() =>
            {
                if (!IsInProgress)
                {
                    return;
                }

                LeaveActiveStep(ExitReason.Cancelled);

                if (State != SequenceState.Faulted)
                {
                    ChangeState(SequenceState.Cancelled);
                    Notify(() => Cancelled?.Invoke());
                }
            });
        }

        private void Execute(Action command)
        {
            if (_isTransitioning)
            {
                _queuedCommands.Enqueue(command);
                return;
            }

            _isTransitioning = true;
            try
            {
                command();
                while (_queuedCommands.Count > 0)
                {
                    _queuedCommands.Dequeue()();
                }
            }
            finally
            {
                _isTransitioning = false;
                _queuedCommands.Clear();
            }

            RethrowHandlerFailures();
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
            _activeStepFinishedWhilePaused = false;
            step.Finished += OnActiveStepFinished;
            step.Failed += OnActiveStepFailed;

            Notify(() => StepEntered?.Invoke(step, index));
            Guard(step.Enter);

            if (State == SequenceState.Paused && _activeStep == step)
            {
                Guard(step.Pause);
            }
        }

        private void OnActiveStepFailed(Exception failure)
        {
            var failingStep = _activeStep;
            if (failingStep == null)
            {
                return;
            }

            Execute(() =>
            {
                if (_activeStep == failingStep)
                {
                    Fault(failure);
                }
            });
        }

        private void OnActiveStepFinished()
        {
            var finishedStep = _activeStep;
            if (finishedStep == null)
            {
                return;
            }

            Execute(() =>
            {
                if (_activeStep != finishedStep)
                {
                    return;
                }

                if (State == SequenceState.Paused)
                {
                    _activeStepFinishedWhilePaused = true;
                }
                else if (State == SequenceState.Running)
                {
                    MoveToNextStep(ExitReason.Completed);
                }
            });
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

            var index = CurrentIndex;
            _activeStep = null;
            step.Finished -= OnActiveStepFinished;
            step.Failed -= OnActiveStepFailed;

            Exception exitFailure = null;
            try
            {
                step.Exit(reason);
            }
            catch (Exception failure)
            {
                exitFailure = failure;
            }

            Notify(() => StepExited?.Invoke(step, index, reason));

            if (exitFailure != null)
            {
                Fault(exitFailure);
            }
        }

        private void FinishSequence()
        {
            ChangeState(SequenceState.Completed);
            Notify(() => Completed?.Invoke());
        }

        private void ChangeState(SequenceState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            Notify(() => StateChanged?.Invoke(state));
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

            var reported = exception;
            var step = _activeStep;
            var index = CurrentIndex;
            _activeStep = null;

            if (step != null)
            {
                step.Finished -= OnActiveStepFinished;
                step.Failed -= OnActiveStepFailed;
                try
                {
                    step.Exit(ExitReason.Cancelled);
                }
                catch (Exception exitFailure)
                {
                    reported = new AggregateException(exception, exitFailure);
                }

                Notify(() => StepExited?.Invoke(step, index, ExitReason.Cancelled));
            }

            ChangeState(SequenceState.Faulted);
            Notify(() => Faulted?.Invoke(reported));
        }

        private void Notify(Action raiseEvent)
        {
            try
            {
                raiseEvent();
            }
            catch (Exception handlerFailure)
            {
                _handlerFailures.Add(handlerFailure);
            }
        }

        private void RethrowHandlerFailures()
        {
            if (_handlerFailures.Count == 0)
            {
                return;
            }

            var failures = _handlerFailures.ToArray();
            _handlerFailures.Clear();
            throw failures.Length == 1 ? failures[0] : new AggregateException(failures);
        }
    }
}
