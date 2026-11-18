using System.Collections.Generic;
using UnityEngine;

namespace Framework.Tasks
{
    public class SimultaneousHighlightTask : HighlightTask
    {
        [SerializeField]
        private SubtaskState _state = SubtaskState.On;

        private readonly Dictionary<Outline, bool> _wasOutlinedBeforeRun = new Dictionary<Outline, bool>();

        public override void Cancel()
        {
            foreach (var outlineAndState in _wasOutlinedBeforeRun)
            {
                if (outlineAndState.Key != null)
                {
                    Apply(outlineAndState.Key, outlineAndState.Value);
                }
            }

            _wasOutlinedBeforeRun.Clear();
        }

        protected override void ApplyHighlight(Outline outline)
        {
            if (!_wasOutlinedBeforeRun.ContainsKey(outline))
            {
                _wasOutlinedBeforeRun[outline] = outline.IsOutLined;
            }

            Apply(outline, SubtaskStateRules.ResolvesToOn(_state, outline.IsOutLined));
        }

        private static void Apply(Outline outline, bool shouldBeOutlined)
        {
            if (shouldBeOutlined)
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
