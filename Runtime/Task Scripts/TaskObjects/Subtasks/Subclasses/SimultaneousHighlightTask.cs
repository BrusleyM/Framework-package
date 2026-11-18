using UnityEngine;

namespace Framework.Tasks
{
    public class SimultaneousHighlightTask : HighlightTask
    {
        [SerializeField]
        private SubtaskState _state = SubtaskState.On;

        protected override void ApplyHighlight(Outline outline)
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
