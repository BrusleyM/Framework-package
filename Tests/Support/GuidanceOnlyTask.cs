using Framework.Tasks;

namespace Framework.Sequencing.Tests
{
    public sealed class GuidanceOnlyTask : Task, IGuidanceOnlyTask
    {
        public int StartCount { get; private set; }

        public override void StartTask() => StartCount++;
    }
}
