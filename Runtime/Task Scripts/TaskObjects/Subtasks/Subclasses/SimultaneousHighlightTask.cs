using System;
using UnityEngine;

namespace Framework.Tasks
{
    public class SimultaneousHighlightTask : HighlightTask
    {
        [SerializeField]
        private SubtaskState _state = SubtaskState.On;

        public override void Execute()
        {
            if (gameObjectsToHighlight == null || gameObjectsToHighlight.Length == 0)
            {
                throw new InvalidOperationException("No GameObjects provided for highlighting.");
            }

            foreach (var highlighted in gameObjectsToHighlight)
            {
                ApplyState(FindOutline(highlighted));
            }

            MarkComplete();
        }

        private void ApplyState(Outline outline)
        {
            if (SubtaskStateRules.ResolvesToOn(_state, outline.IsOutLined))
            {
                outline.EnableOutline();
            }
            else
            {
                outline.DisableOutline();
            }
        }
    }
}
