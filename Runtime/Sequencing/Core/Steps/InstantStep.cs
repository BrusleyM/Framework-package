using System;

namespace Framework.Sequencing
{
    public sealed class InstantStep : StepBase
    {
        private readonly Action _whenEntered;
        private readonly Action<ExitReason> _whenExited;

        public InstantStep(string name, Action whenEntered, Action<ExitReason> whenExited = null) : base(name)
        {
            _whenEntered = whenEntered;
            _whenExited = whenExited;
        }

        protected override void OnEnter()
        {
            _whenEntered?.Invoke();
            Finish();
        }

        protected override void OnExit(ExitReason reason)
        {
            _whenExited?.Invoke(reason);
        }
    }
}
