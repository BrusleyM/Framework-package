using Framework.Tasks;
using UnityEditor;
using UnityEngine;

namespace Framework.Sequencing.Editor
{
    public static class SequenceSetupMenu
    {
        [MenuItem("Tools/Framework/Check sequence setup")]
        public static void CheckOpenScenes()
        {
            var found = 0;
            foreach (var sequence in Object.FindObjectsOfType<SequentialTask>(true))
            {
                found += Report(sequence, sequence.GetSetupIssues());
            }

            foreach (var task in Object.FindObjectsOfType<GuidedTask>(true))
            {
                found += Report(task, task.GetSetupIssues());
            }

            Debug.Log(found == 0 ? "Sequence setup check: no issues found." : $"Sequence setup check: {found} issue(s) found.");
        }

        private static int Report(Object context, System.Collections.Generic.IReadOnlyList<SetupIssue> issues)
        {
            foreach (var issue in issues)
            {
                Debug.LogWarning($"{context.name}: {issue.Message}", context);
            }

            return issues.Count;
        }
    }
}
