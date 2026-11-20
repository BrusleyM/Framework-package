using System;
using Framework.Tasks;

namespace Framework.Sequencing.Tests
{
    public sealed class ThrowingSubtask : GameObjectTask
    {
        public override void Execute()
        {
            throw new InvalidOperationException("missing reference");
        }
    }
}
