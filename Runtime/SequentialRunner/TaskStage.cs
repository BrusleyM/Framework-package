using System.Collections.Generic;
using Framework.Tasks;
using UnityEngine;

namespace Framework.Sequencing
{
    public class TaskStage : MonoBehaviour
    {
        [SerializeField]
        private string _stageName;
        [SerializeField]
        private List<Task> _tasks = new List<Task>();

        public string StageName => string.IsNullOrEmpty(_stageName) ? name : _stageName;
        public IReadOnlyList<Task> Tasks => _tasks;

        public void AddTask(Task task)
        {
            _tasks.Add(task);
        }
    }
}
