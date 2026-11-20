using UnityEngine;

namespace Framework.Tasks
{
    public sealed class UnscaledTaskClock : ITaskClock
    {
        public object WaitFor(float seconds)
        {
            return new WaitForSecondsRealtime(seconds);
        }
    }
}
