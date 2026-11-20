using Framework.Tasks;
using UnityEditor;

namespace Framework.Sequencing.Editor
{
    [CustomEditor(typeof(GuidedTask), true)]
    public sealed class GuidedTaskInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SetupIssueBoxes.Draw(((GuidedTask)target).GetSetupIssues());
            DrawDefaultInspector();
        }
    }
}
