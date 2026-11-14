using System;
using UnityEngine;

namespace Framework.Tasks
{
    public class EnableTask : GameObjectTask
    {
        [SerializeField]
        private GameObject gameObjectToEnable;

        public override void Execute()
        {
            EnableGameObject();
            MarkComplete();
        }

        public void DisableGameObject()
        {
            if (gameObjectToEnable != null)
            {
                gameObjectToEnable.SetActive(false);
            }

            MarkComplete();
        }

        protected void EnableGameObject()
        {
            if (gameObjectToEnable == null)
            {
                throw new InvalidOperationException("No GameObject assigned to EnableTask.");
            }

            gameObjectToEnable.SetActive(true);
        }
    }
}
