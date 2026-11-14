namespace Framework.Sequencing
{
    public sealed class WaitForConditionStep : StepBase
    {
        private readonly IStepCondition _condition;

        public WaitForConditionStep(string name, IStepCondition condition) : base(name)
        {
            _condition = condition;
        }

        protected override void OnEnter()
        {
            _condition.Satisfied += Finish;
            _condition.Arm();
        }

        protected override void OnExit(ExitReason reason)
        {
            _condition.Satisfied -= Finish;
            _condition.Disarm();
        }
    }
}
