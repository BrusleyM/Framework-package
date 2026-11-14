namespace Framework.Sequencing
{
    public sealed class DelayStep : StepBase
    {
        private readonly float _durationSeconds;
        private float _elapsedSeconds;

        public DelayStep(string name, float durationSeconds) : base(name)
        {
            _durationSeconds = durationSeconds;
        }

        protected override void OnEnter()
        {
            _elapsedSeconds = 0f;
            if (_durationSeconds <= 0f)
            {
                Finish();
            }
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            _elapsedSeconds += deltaSeconds;
            if (_elapsedSeconds >= _durationSeconds)
            {
                Finish();
            }
        }
    }
}
