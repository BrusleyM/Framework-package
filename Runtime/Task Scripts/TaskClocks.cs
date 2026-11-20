namespace Framework.Tasks
{
    public static class TaskClocks
    {
        public static readonly ITaskClock Scaled = new ScaledTaskClock();
        public static readonly ITaskClock Unscaled = new UnscaledTaskClock();

        public static ITaskClock For(bool ignoreTimeScale)
        {
            return ignoreTimeScale ? Unscaled : Scaled;
        }
    }
}
