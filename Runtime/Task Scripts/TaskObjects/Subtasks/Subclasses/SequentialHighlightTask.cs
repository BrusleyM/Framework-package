using System.Collections;
using UnityEngine;

namespace Framework.Tasks
{
    public class SequentialHighlightTask : HighlightTask
    {
        private Coroutine _highlightRoutine;

        public override void Cancel()
        {
            StopHighlighting();
            base.Cancel();
        }

        protected override void EnableOutline()
        {
            StopHighlighting();
            _highlightRoutine = StartCoroutine(HighlightOneAfterAnother());
        }

        private void StopHighlighting()
        {
            if (_highlightRoutine != null)
            {
                StopCoroutine(_highlightRoutine);
                _highlightRoutine = null;
            }
        }

        private IEnumerator HighlightOneAfterAnother()
        {
            foreach (var highlighted in gameObjectsToHighlight)
            {
                var outline = FindOutline(highlighted);
                outline.EnableOutline();
                yield return new WaitUntil(() => !outline.IsOutLined);
                MarkComplete();
            }

            _highlightRoutine = null;
        }
    }
}
