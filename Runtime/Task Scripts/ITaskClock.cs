namespace Framework.Tasks
{
    public interface ITaskClock
    {
        object WaitFor(float seconds);
    }
}
