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

        private bool _hasStateBeforeRun;
        private bool _wasActiveBeforeRun;

        public override void Execute()
        {
            ApplyState();
            MarkComplete();
        }

        public override void Cancel()
        {
            if (_hasStateBeforeRun && gameObjectToEnable != null)
            {
                gameObjectToEnable.SetActive(_wasActiveBeforeRun);
            }

            _hasStateBeforeRun = false;
        }

        private void ApplyState()
        {
            if (gameObjectToEnable == null)
            {
                throw new InvalidOperationException("No GameObject assigned to EnableTask.");
            }

            if (!_hasStateBeforeRun)
            {
                _wasActiveBeforeRun = gameObjectToEnable.activeSelf;
                _hasStateBeforeRun = true;
            }

            gameObjectToEnable.SetActive(SubtaskStateRules.ResolvesToOn(_state, gameObjectToEnable.activeSelf));
        }
    }
}
