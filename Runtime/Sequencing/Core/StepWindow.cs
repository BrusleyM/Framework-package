namespace Framework.Sequencing
{
    public readonly struct StepWindow
    {
        private StepWindow(string previousName, string currentName, string nextName, int position, int count)
        {
            PreviousName = previousName;
            CurrentName = currentName;
            NextName = nextName;
            Position = position;
            Count = count;
        }

        public string PreviousName { get; }
        public string CurrentName { get; }
        public string NextName { get; }
        public int Position { get; }
        public int Count { get; }

        public static StepWindow Of(SequenceRunner runner)
        {
            var steps = runner.Steps;
            var index = runner.CurrentIndex;
            var hasActiveStep = runner.CurrentStep != null;

            var previousIndex = hasActiveStep ? index - 1 : index;
            var nextIndex = index < 0 ? 0 : index + 1;

            return new StepWindow(
                NameAt(steps, previousIndex),
                hasActiveStep ? steps[index].Name : null,
                NameAt(steps, nextIndex),
                hasActiveStep ? index + 1 : 0,
                steps.Count);
        }

        private static string NameAt(System.Collections.Generic.IReadOnlyList<ISequenceStep> steps, int index)
        {
            return index >= 0 && index < steps.Count ? steps[index].Name : null;
        }
    }
}
