using System;
using UnityEngine;

namespace Framework.Tasks
{
    public abstract class HighlightTask : GameObjectTask
    {
        [SerializeField]
        protected GameObject[] gameObjectsToHighlight;

        public override void Execute()
        {
            EnableOutline();
        }

        public virtual void DisableAllOutline()
        {
            foreach (var highlighted in gameObjectsToHighlight)
            {
                DisableOutline(highlighted);
            }

            MarkComplete();
        }

        public virtual void DisableOutline(GameObject highlighted)
        {
            if (highlighted == null)
            {
                return;
            }

            var outline = highlighted.GetComponent<Outline>();
            if (outline != null)
            {
                outline.DisableOutline();
            }
        }

        public override void Cancel()
        {
            foreach (var highlighted in gameObjectsToHighlight)
            {
                DisableOutline(highlighted);
            }
        }

        protected virtual void EnableOutline()
        {
            if (gameObjectsToHighlight == null || gameObjectsToHighlight.Length == 0)
            {
                throw new InvalidOperationException("No GameObjects provided for highlighting.");
            }

            foreach (var highlighted in gameObjectsToHighlight)
            {
                ApplyHighlight(FindOutline(highlighted));
            }

            MarkComplete();
        }

        protected virtual void ApplyHighlight(Outline outline)
        {
            outline.EnableOutline();
        }

        protected static Outline FindOutline(GameObject highlighted)
        {
            var outline = highlighted != null ? highlighted.GetComponent<Outline>() : null;
            if (outline == null)
            {
                throw new InvalidOperationException("Outline component not found on GameObject.");
            }

            return outline;
        }
    }
}
