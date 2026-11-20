using Framework.Tasks;

namespace Framework.Sequencing.Tests
{
    internal sealed class ProbeTask : Task, IUnguidedTask, IHintable, IPausableTask
    {
        public string Label { get; set; }
        public int StartCount { get; private set; }
        public int CancelCount { get; private set; }
        public int UnguidedStartCount { get; private set; }
        public int HintShowCount { get; private set; }
        public int PauseCount { get; private set; }
        public int ResumeCount { get; private set; }

        public void Finish() => TaskComplete();

        public override void StartTask() => StartCount++;

        public void StartTaskUnguided() => UnguidedStartCount++;

        public void ShowHint() => HintShowCount++;

        public void HideHint()
        {
        }

        public override void CancelTask() => CancelCount++;

        public void PauseTask() => PauseCount++;

        public void ResumeTask() => ResumeCount++;
    }

    internal sealed class GuidanceOnlyProbeTask : Task, IGuidanceOnlyTask
    {
        public int StartCount { get; private set; }
        public int UnguidedStartCount { get; private set; }

        public override void StartTask() => StartCount++;
    }
}
