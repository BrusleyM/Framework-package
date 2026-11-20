using Framework.Tasks;

namespace Framework.Sequencing.Tests
{
    public sealed class PlainTask : Task
    {
        public int StartCount { get; private set; }

        public override void StartTask() => StartCount++;
    }
}
