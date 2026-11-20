using System;
using System.Collections.Generic;
using Framework.CheckPoints;
using UnityEngine;

namespace Framework.Tasks
{
    public class ActionTask : GuidedTask
    {
        [SerializeField]
        private CheckPointsParent _checkPoints;

        public CheckPointsParent CheckPointsParent => _checkPoints;

        protected override IEnumerable<SetupIssue> GetCompletionSetupIssues(bool forUnguidedMode)
        {
            var issues = new List<SetupIssue>();
            if (!HasCheckPoints())
            {
                issues.Add(new SetupIssue("No check points are assigned, so the task can never complete."));
            }

            foreach (var subtask in SubTasks)
            {
                if (forUnguidedMode && subtask is HighlightTask && subtask.IsSetup)
                {
                    issues.Add(new SetupIssue($"Subtask '{subtask.name}' is a highlight marked Is Setup, so it is shown even without a hint: untick Is Setup so it only shows when guided or on a hint."));
                }
            }

            return issues;
        }

        protected override void BeginCompletionTracking()
        {
            if (_checkPoints == null)
            {
                throw new InvalidOperationException($"{name} has no check points assigned.");
            }

            LinkCheckPointsToThisTask();
            _checkPoints.ResetAllPoints();
            _checkPoints.OnAllChecked.AddListener(TaskComplete);
        }

        protected override void EndCompletionTracking()
        {
            _checkPoints?.OnAllChecked?.RemoveListener(TaskComplete);
        }

        protected override void ResetCompletionTracking()
        {
            _checkPoints?.ResetAllPoints();
        }

        private bool HasCheckPoints()
        {
            if (_checkPoints == null || _checkPoints.CheckPoints == null)
            {
                return false;
            }

            foreach (var point in _checkPoints.CheckPoints)
            {
                if (point != null)
                {
                    return true;
                }
            }

            return false;
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
