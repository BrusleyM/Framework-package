namespace Framework.Sequencing
{
    public sealed class StageStep : StepBase
    {
        public StageStep(string name, SequenceRunner stage) : base(name)
        {
            Stage = stage;
        }

        public SequenceRunner Stage { get; }

        protected override void OnEnter()
        {
            Stage.Completed += Finish;
            Stage.Faulted += Fail;
            Stage.Restart();
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            Stage.Advance(deltaSeconds);
        }

        protected override void OnPause()
        {
            Stage.Pause();
        }

        protected override void OnResume()
        {
            Stage.Resume();
        }

        protected override void OnExit(ExitReason reason)
        {
            Stage.Completed -= Finish;
            Stage.Faulted -= Fail;
            Stage.Cancel();
        }
    }
}
