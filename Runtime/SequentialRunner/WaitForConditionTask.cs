using Framework.Tasks;
using UnityEngine;

namespace Framework.Sequencing
{
    public class WaitForConditionTask : Task
    {
        [SerializeField]
        private MonoBehaviour _conditionSource;

        private IStepCondition Condition => _conditionSource as IStepCondition;

        public override void StartTask()
        {
            if (Condition == null)
            {
                throw new System.InvalidOperationException($"{name} needs a condition source that implements IStepCondition.");
            }

            Condition.Satisfied -= TaskComplete;
            Condition.Satisfied += TaskComplete;

            try
            {
                Condition.Arm();
            }
            catch
            {
                Condition.Satisfied -= TaskComplete;
                throw;
            }
        }

        public override void TaskComplete()
        {
            StopWaiting();
            base.TaskComplete();
        }

        public override void Skip()
        {
            StopWaiting();
            base.Skip();
        }

        public override void CancelTask()
        {
            StopWaiting();
        }

        private void StopWaiting()
        {
            if (Condition == null)
            {
                return;
            }

            Condition.Satisfied -= TaskComplete;
            Condition.Disarm();
        }

        private void OnValidate()
        {
            if (_conditionSource != null && !(_conditionSource is IStepCondition))
            {
                Debug.LogError($"{_conditionSource.name} does not implement IStepCondition.", this);
                _conditionSource = null;
            }
        }
    }
}
