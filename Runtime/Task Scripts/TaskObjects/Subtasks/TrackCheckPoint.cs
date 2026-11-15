using UnityEngine;

namespace Framework.Tasks
{
    public class TrackCheckPoint : GameObjectTask
    {
        [SerializeField]
        private ActionTask _taskPoints;

        public override void Execute()
        {
            StopTracking();
            _taskPoints.CheckPointsParent.OnAllChecked.AddListener(CompleteAndStopTracking);
        }

        public override void Cancel()
        {
            StopTracking();
        }

        private void CompleteAndStopTracking()
        {
            StopTracking();
            MarkComplete();
        }

        private void StopTracking()
        {
            _taskPoints.CheckPointsParent.OnAllChecked.RemoveListener(CompleteAndStopTracking);
        }
    }
}
