using System.Collections.Generic;
using Framework.Tasks;
using UnityEditor;
using UnityEngine;

namespace Framework.Sequencing.Editor
{
    internal static class SetupIssueBoxes
    {
        private static readonly Dictionary<int, IReadOnlyList<SetupIssue>> IssuesAtLastLayout = new Dictionary<int, IReadOnlyList<SetupIssue>>();

        public static void Draw(Object target, System.Func<IReadOnlyList<SetupIssue>> check)
        {
            var id = target.GetInstanceID();
            var isLayout = Event.current != null && Event.current.type == EventType.Layout;
            if (isLayout || !IssuesAtLastLayout.ContainsKey(id))
            {
                IssuesAtLastLayout[id] = check();
            }

            foreach (var issue in IssuesAtLastLayout[id])
            {
                EditorGUILayout.HelpBox(issue.Message, MessageType.Warning);
            }
        }
    }
}
