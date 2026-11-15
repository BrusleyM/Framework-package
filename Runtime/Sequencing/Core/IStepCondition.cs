using System;

namespace Framework.Sequencing
{
    public interface IStepCondition
    {
        event Action Satisfied;
        void Arm();
        void Disarm();
    }
}
