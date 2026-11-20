using UnityEngine;

namespace Framework.Tasks
{
    public sealed class ScaledTaskClock : ITaskClock
    {
        public object WaitFor(float seconds)
        {
            return new WaitForSeconds(seconds);
        }
    }
}
