using Framework.Tasks;

namespace Framework.Sequencing.Tests
{
    internal sealed class ProbeTask : Task
    {
        public string Label { get; set; }
        public int StartCount { get; private set; }
        public int CancelCount { get; private set; }
        public int PauseCount { get; private set; }
        public int ResumeCount { get; private set; }

        public void Finish() => TaskComplete();

        public override void StartTask() => StartCount++;

        public override void CancelTask() => CancelCount++;

        public override void PauseTask() => PauseCount++;

        public override void ResumeTask() => ResumeCount++;
    }
}
