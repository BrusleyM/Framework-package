using System.Collections.Generic;
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
            var covered = new HashSet<Task>();
            foreach (var sequence in Object.FindObjectsByType<SequentialTask>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                found += Report(sequence, sequence.GetSetupIssues());
                foreach (var task in sequence.AllTasks)
                {
                    covered.Add(task);
                }
            }

            foreach (var task in Object.FindObjectsByType<GuidedTask>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                if (!covered.Contains(task))
                {
                    found += Report(task, task.GetSetupIssues());
                }
            }

            Debug.Log(found == 0 ? "Sequence setup check: no issues found." : $"Sequence setup check: {found} issue(s) found.");
        }

        private static int Report(Object context, IReadOnlyList<SetupIssue> issues)
        {
            foreach (var issue in issues)
            {
                Debug.LogWarning($"{context.name}: {issue.Message}", context);
            }

            return issues.Count;
        }
    }
}
