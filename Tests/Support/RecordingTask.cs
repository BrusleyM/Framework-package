using Framework.Tasks;

namespace Framework.Sequencing.Tests
{
    public sealed class RecordingTask : Task, IUnguidedTask, IHintable, IPausableTask, ISettlingTask
    {
        public string Label { get; set; }
        public int StartCount { get; private set; }
        public int GuidedStartCount => StartCount;
        public int UnguidedStartCount { get; private set; }
        public int CancelCount { get; private set; }
        public int HintShowCount { get; private set; }
        public int HintHideCount { get; private set; }
        public int PauseCount { get; private set; }
        public int ResumeCount { get; private set; }
        public bool FinishWhenStarted { get; set; }
        public bool Settling { get; set; }

        public bool IsSettling => Settling;

        public void Finish() => TaskComplete();

        public override void StartTask() => StartCount++;

        public void StartTaskUnguided()
        {
            UnguidedStartCount++;
            if (FinishWhenStarted)
            {
                TaskComplete();
            }
        }

        public override void CancelTask() => CancelCount++;

        public void PauseTask() => PauseCount++;

        public void ResumeTask() => ResumeCount++;

        public void ShowHint() => HintShowCount++;

        public void HideHint() => HintHideCount++;
    }
}
