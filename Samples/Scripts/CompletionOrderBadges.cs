using System;
using System.Collections.Generic;
using Framework.Tasks;
using UnityEngine;

namespace FrameworkTest
{
    public class CompletionOrderBadges : MonoBehaviour
    {
        [Serializable]
        private class Badge
        {
            public Task Task;
            public TextMesh Label;
        }

        [SerializeField]
        private List<Badge> _badges = new List<Badge>();
        [SerializeField]
        private TextMesh _result;
        [SerializeField]
        private string _inOrderText = "Listed order";
        [SerializeField]
        private string _outOfOrderText = "Your own order";

        private void Awake()
        {
            Clear();
        }

        public void ShowPosition(Task task, int position)
        {
            foreach (var badge in _badges)
            {
                if (badge.Task == task && badge.Label != null)
                {
                    badge.Label.text = position.ToString();
                }
            }
        }

        public void ShowInOrder()
        {
            Say(_inOrderText);
        }

        public void ShowOutOfOrder()
        {
            Say(_outOfOrderText);
        }

        public void Clear()
        {
            foreach (var badge in _badges)
            {
                if (badge.Label != null)
                {
                    badge.Label.text = string.Empty;
                }
            }
            Say(string.Empty);
        }

        private void Say(string message)
        {
            if (_result != null)
            {
                _result.text = message;
            }
        }
    }
}
