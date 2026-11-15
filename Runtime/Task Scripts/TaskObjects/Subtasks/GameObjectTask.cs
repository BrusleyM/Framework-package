using System;
using UnityEngine;

namespace Framework.Tasks
{
    public abstract class GameObjectTask : MonoBehaviour
    {
        public bool IsComplete { get; private set; }

        public event Action Completed;

        public void Prepare()
        {
            IsComplete = false;
        }

        public abstract void Execute();

        public virtual void Cancel()
        {
        }

        protected void MarkComplete()
        {
            if (IsComplete)
            {
                return;
            }

            IsComplete = true;
            Completed?.Invoke();
        }
    }
}
