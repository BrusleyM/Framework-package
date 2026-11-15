using System;

namespace Framework.Sequencing
{
    public interface ISequenceStep
    {
        string Name { get; }
        event Action Finished;
        event Action<Exception> Failed;
        void Enter();
        void Advance(float deltaSeconds);
        void Pause();
        void Resume();
        void Exit(ExitReason reason);
    }
}
