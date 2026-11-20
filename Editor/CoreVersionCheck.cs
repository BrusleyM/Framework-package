using System.IO;
using Framework.Editor;
using UnityEditor;
using UnityEngine;

namespace Framework.Sequencing.Editor
{
    [InitializeOnLoad]
    public static class CoreVersionCheck
    {
        private const string SequencerEditorAssembly = "Framework.Sequencing.Unity.Editor";
        private const string CoreEditorAssembly = "Framework.Core.Editor";
        private const string CoreName = "Framework Core";
        private const string SequencerName = "Framework Sequencer";

        static CoreVersionCheck()
        {
            EditorApplication.delayCall += WarnWhenTheInstalledCoreIsTooOld;
        }

        public static string FindProblem(string sequencerManifestJson, string coreManifestJson)
        {
            var sequencer = SampleImporter.ReadPackage(sequencerManifestJson);
            var core = SampleImporter.ReadPackage(coreManifestJson);
            return PackageCompatibility.FindProblem(SequencerName, CoreName, core.version, sequencer.requiresCore);
        }

        private static void WarnWhenTheInstalledCoreIsTooOld()
        {
            var sequencerRoot = SampleImporter.FindPackageRoot(SequencerEditorAssembly);
            var coreRoot = SampleImporter.FindPackageRoot(CoreEditorAssembly);
            if (sequencerRoot == null || coreRoot == null)
            {
                return;
            }

            var problem = FindProblem(File.ReadAllText(Path.Combine(sequencerRoot, "package.json")), File.ReadAllText(Path.Combine(coreRoot, "package.json")));
            if (problem != null)
            {
                Debug.LogWarning(problem);
            }
        }
    }
}
