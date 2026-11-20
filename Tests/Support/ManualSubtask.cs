using System;
using Framework.Tasks;

namespace Framework.Sequencing.Tests
{
    public sealed class ManualSubtask : GameObjectTask
    {
        public int ExecuteCount { get; private set; }
        public int CancelCount { get; private set; }
        public Action OnExecute { get; set; }
        public Action OnCancel { get; set; }

        public override void Execute()
        {
            ExecuteCount++;
            OnExecute?.Invoke();
        }

        public override void Cancel()
        {
            CancelCount++;
            OnCancel?.Invoke();
        }

        public void Finish()
        {
            MarkComplete();
        }
    }
}
