using Framework.Editor;
using UnityEditor;

namespace Framework.Sequencing.Editor
{
    public static class ImportSequencerSamplesMenu
    {
        [MenuItem("Tools/Framework/Import sequencer samples")]
        public static void Import()
        {
            SampleImporter.ImportAll(SampleImporter.FindPackageRoot("Framework.Sequencing.Unity.Editor"), "Framework Sequencer");
        }
    }
}
