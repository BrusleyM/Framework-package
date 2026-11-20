using System.Collections.Generic;
using Framework.Tasks;
using UnityEditor;

namespace Framework.Sequencing.Editor
{
    internal static class SetupIssueBoxes
    {
        public static void Draw(IReadOnlyList<SetupIssue> issues)
        {
            foreach (var issue in issues)
            {
                EditorGUILayout.HelpBox(issue.Message, MessageType.Warning);
            }
        }
    }
}
