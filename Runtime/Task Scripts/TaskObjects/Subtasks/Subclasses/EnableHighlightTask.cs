using UnityEngine;

namespace Framework.Tasks
{
    public class EnableAndHighlightTask : GameObjectTask
    {
        [SerializeField]
        private GameObject gameObjectToEnable;
        [SerializeField]
        private HighlightTask highlightTask;

        public override void Execute()
        {
            gameObjectToEnable.SetActive(true);
            highlightTask.Prepare();
            highlightTask.Completed -= MarkComplete;
            highlightTask.Completed += MarkComplete;
            highlightTask.Execute();
        }

        public override void Cancel()
        {
            highlightTask.Completed -= MarkComplete;
            highlightTask.Cancel();
        }
    }
}
