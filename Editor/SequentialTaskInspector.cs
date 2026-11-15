using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Framework.Sequencing.Editor
{
    [CustomEditor(typeof(SequentialTask), true)]
    public sealed class SequentialTaskInspector : UnityEditor.Editor
    {
        private readonly List<SequenceRunner> _watchedRunners = new List<SequenceRunner>();

        private void OnDisable()
        {
            StopWatchingEveryRunner();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sequence", EditorStyles.boldLabel);

            var task = (SequentialTask)target;
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter play mode to watch and control the sequence.", MessageType.Info);
                return;
            }

            if (task.Runner == null)
            {
                DrawRunControl(task);
                return;
            }

            WatchRunnersOf(task);
            DrawState(task.Runner);
            DrawWindows(task.Runner);
            DrawControls(task);
        }

        private static void DrawRunControl(SequentialTask task)
        {
            EditorGUILayout.HelpBox("Nothing has been played yet.", MessageType.None);
            if (GUILayout.Button("Run task list"))
            {
                task.Run();
            }
        }

        private static void DrawState(SequenceRunner runner)
        {
            EditorGUILayout.LabelField(runner.Name, runner.State.ToString());
        }

        private static void DrawWindows(SequenceRunner runner)
        {
            if (runner.CurrentStep is StageStep stage)
            {
                DrawWindow("Stage", StepWindow.Of(runner));
                DrawWindow("Step", StepWindow.Of(stage.Stage));
                return;
            }

            DrawWindow("Step", StepWindow.Of(runner));
        }

        private static void DrawWindow(string title, StepWindow window)
        {
            EditorGUILayout.LabelField($"{title}  {window.Position} / {window.Count}", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSlot("Previous", window.PreviousName, isCurrent: false);
                DrawSlot("Current", window.CurrentName, isCurrent: true);
                DrawSlot("Next", window.NextName, isCurrent: false);
            }
        }

        private static void DrawSlot(string caption, string stepName, bool isCurrent)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(caption, EditorStyles.miniLabel);
                var style = isCurrent ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUILayout.LabelField(stepName ?? "-", new GUIStyle(style) { wordWrap = true });
            }
        }

        private static void DrawControls(SequentialTask task)
        {
            var runner = task.Runner;
            var isRunning = runner.State == SequenceState.Running;
            var isPaused = runner.State == SequenceState.Paused;
            var isInProgress = runner.IsInProgress;
            var hasStages = runner.CurrentStep is StageStep;

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!isInProgress))
            {
                DrawButtonRow("Step", ("Back", task.GoBack), ("Skip", task.SkipCurrentTask));
                if (hasStages)
                {
                    DrawButtonRow("Stage", ("Back", task.GoBackStage), ("Restart", task.RestartCurrentStage), ("Skip", task.SkipCurrentStage));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!isRunning))
                {
                    if (GUILayout.Button("Pause"))
                    {
                        task.Pause();
                    }
                }

                using (new EditorGUI.DisabledScope(!isPaused))
                {
                    if (GUILayout.Button("Resume"))
                    {
                        task.Resume();
                    }
                }

                if (GUILayout.Button("Restart"))
                {
                    task.Restart();
                }

                using (new EditorGUI.DisabledScope(!isInProgress))
                {
                    if (GUILayout.Button("Cancel"))
                    {
                        task.Cancel();
                    }
                }
            }
        }

        private static void DrawButtonRow(string title, params (string label, System.Action action)[] buttons)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, GUILayout.Width(40f));
                foreach (var (label, action) in buttons)
                {
                    if (GUILayout.Button(label))
                    {
                        action();
                    }
                }
            }
        }

        private void WatchRunnersOf(SequentialTask task)
        {
            var wanted = new List<SequenceRunner> { task.Runner };
            if (task.Runner.CurrentStep is StageStep stage)
            {
                wanted.Add(stage.Stage);
            }

            if (wanted.Count == _watchedRunners.Count && wanted.TrueForAll(_watchedRunners.Contains))
            {
                return;
            }

            StopWatchingEveryRunner();
            foreach (var runner in wanted)
            {
                runner.StepEntered += RepaintOnStepEntered;
                runner.StateChanged += RepaintOnStateChanged;
                _watchedRunners.Add(runner);
            }
        }

        private void StopWatchingEveryRunner()
        {
            foreach (var runner in _watchedRunners)
            {
                runner.StepEntered -= RepaintOnStepEntered;
                runner.StateChanged -= RepaintOnStateChanged;
            }

            _watchedRunners.Clear();
        }

        private void RepaintOnStepEntered(ISequenceStep step, int index)
        {
            Repaint();
        }

        private void RepaintOnStateChanged(SequenceState state)
        {
            Repaint();
        }
    }
}
