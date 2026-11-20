using Framework.Tasks;
using UnityEditor;

namespace Framework.Sequencing.Editor
{
    [InitializeOnLoad]
    internal static class GuidedTaskHeader
    {
        static GuidedTaskHeader()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += DrawSetupIssues;
        }

        private static void DrawSetupIssues(UnityEditor.Editor editor)
        {
            if (editor.target is GuidedTask task)
            {
                SetupIssueBoxes.Draw(task, () => task.GetSetupIssues());
            }
        }
    }
}
