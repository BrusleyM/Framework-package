using UnityEngine;

namespace Framework.Tasks
{
    public class SequentialHighlightTask : HighlightTask
    {
        private Outline _currentOutline;
        private int _highlightIndex;

        public override void Cancel()
        {
            StopWatchingCurrentOutline();
            base.Cancel();
        }

        protected override void EnableOutline()
        {
            _highlightIndex = -1;
            HighlightNext();
        }

        private void HighlightNext()
        {
            StopWatchingCurrentOutline();

            while (++_highlightIndex < gameObjectsToHighlight.Length)
            {
                var isFirst = _highlightIndex == 0;
                var outline = isFirst ? FindOutline(gameObjectsToHighlight[0]) : FindOutlineIfStillAlive(gameObjectsToHighlight[_highlightIndex]);

                if (outline == null)
                {
                    continue;
                }

                _currentOutline = outline;
                _currentOutline.Disabled += OnCurrentOutlineDisabled;
                _currentOutline.EnableOutline();
                return;
            }
        }

        private void OnCurrentOutlineDisabled()
        {
            MarkComplete();
            HighlightNext();
        }

        private void StopWatchingCurrentOutline()
        {
            if (_currentOutline != null)
            {
                _currentOutline.Disabled -= OnCurrentOutlineDisabled;
                _currentOutline = null;
            }
        }

        private static Outline FindOutlineIfStillAlive(GameObject highlighted)
        {
            return highlighted != null ? highlighted.GetComponent<Outline>() : null;
        }
    }
}
