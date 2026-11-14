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
            _highlightIndex++;

            if (_highlightIndex >= gameObjectsToHighlight.Length)
            {
                return;
            }

            _currentOutline = FindOutline(gameObjectsToHighlight[_highlightIndex]);
            _currentOutline.Disabled += OnCurrentOutlineDisabled;
            _currentOutline.EnableOutline();
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
    }
}
