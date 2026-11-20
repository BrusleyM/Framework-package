using Framework.CheckPoints;
using UnityEngine;

namespace Framework.Tasks
{
    public class ActionTask : GuidedTask
    {
        [SerializeField]
        private CheckPointsParent _checkPoints;

        public CheckPointsParent CheckPointsParent => _checkPoints;

        protected override void BeginCompletionTracking()
        {
            LinkCheckPointsToThisTask();
            _checkPoints.ResetAllPoints();
            _checkPoints.OnAllChecked.AddListener(TaskComplete);
        }

        protected override void EndCompletionTracking()
        {
            _checkPoints.OnAllChecked.RemoveListener(TaskComplete);
        }

        protected override void ResetCompletionTracking()
        {
            _checkPoints.ResetAllPoints();
        }

        private void LinkCheckPointsToThisTask()
        {
            foreach (var point in _checkPoints.CheckPoints)
            {
                if (point != null)
                {
                    point.Parent = _checkPoints;
                }
            }
        }
    }
}
