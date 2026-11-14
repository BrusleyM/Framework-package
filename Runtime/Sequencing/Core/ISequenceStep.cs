using System;

namespace Framework.Sequencing
{
    public interface ISequenceStep
    {
        string Name { get; }
        event Action Finished;
        void Enter();
        void Advance(float deltaSeconds);
        void Exit(ExitReason reason);
    }
}
