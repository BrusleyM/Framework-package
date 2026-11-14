using System;
using System.Collections.Generic;

namespace Framework.Sequencing
{
    public sealed class SequenceBuilder
    {
        private readonly string _sequenceName;
        private readonly List<ISequenceStep> _steps = new List<ISequenceStep>();

        internal SequenceBuilder(string sequenceName)
        {
            _sequenceName = sequenceName;
        }

        public SequenceBuilder Then(ISequenceStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            _steps.Add(step);
            return this;
        }

        public SequenceBuilder Do(string stepName, Action whenEntered)
        {
            return Then(new InstantStep(stepName, whenEntered));
        }

        public SequenceBuilder Wait(float seconds)
        {
            return Then(new DelayStep(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Wait {0}s", seconds), seconds));
        }

        public SequenceBuilder WaitFor(string stepName, IStepCondition condition)
        {
            return Then(new WaitForConditionStep(stepName, condition));
        }

        public SequenceBuilder Together(string stepName, params ISequenceStep[] steps)
        {
            return Then(new ParallelStep(stepName, steps));
        }

        public SequenceRunner Build()
        {
            return new SequenceRunner(_sequenceName, _steps);
        }
    }
}
