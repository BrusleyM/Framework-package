namespace Framework.Tasks
{
    public interface IPausableTask
    {
        void PauseTask();

        void ResumeTask();
    }
}
