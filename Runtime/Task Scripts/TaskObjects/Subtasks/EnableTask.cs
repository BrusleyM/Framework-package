using System;
using UnityEngine;

namespace Framework.Tasks
{
    public class EnableTask : GameObjectTask
    {
        [SerializeField]
        private GameObject gameObjectToEnable;
        [SerializeField]
        private SubtaskState _state = SubtaskState.On;

        public override void Execute()
        {
            ApplyState();
            MarkComplete();
        }

        public override void Cancel()
        {
            DeactivateGameObject();
        }

        public void DisableGameObject()
        {
            DeactivateGameObject();
            MarkComplete();
        }

        private void DeactivateGameObject()
        {
            if (gameObjectToEnable != null)
            {
                gameObjectToEnable.SetActive(false);
            }
        }

        private void ApplyState()
        {
            if (gameObjectToEnable == null)
            {
                throw new InvalidOperationException("No GameObject assigned to EnableTask.");
            }

            gameObjectToEnable.SetActive(SubtaskStateRules.ResolvesToOn(_state, gameObjectToEnable.activeSelf));
        }
    }
}
