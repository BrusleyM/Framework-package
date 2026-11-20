using System.Collections.Generic;
using Framework.Tasks;

namespace Framework.Sequencing
{
    public static class SequenceSetupRules
    {
        public static IReadOnlyList<SetupIssue> Check(
            GuidanceMode mode,
            IReadOnlyList<Task> tasks,
            IReadOnlyList<TaskStage> stages,
            bool hasAudioSource,
            bool introHasClips,
            bool completeHasClips,
            float hintAfterIdleSeconds)
        {
            var issues = new List<SetupIssue>();
            var groups = new List<List<Task>>();
            var listed = new HashSet<Task>();
            if (stages.Count == 0)
            {
                groups.Add(CollectTasks("Task List", tasks, listed, issues));
                if (tasks.Count == 0)
                {
                    issues.Add(new SetupIssue("The task list is empty, so the sequence completes at once."));
                }
            }
            else
            {
                CheckStages(stages, groups, listed, issues);
            }

            CheckAudio("Intro Audio", introHasClips, hasAudioSource, issues);
            CheckAudio("Complete Audio", completeHasClips, hasAudioSource, issues);
            CheckTasks(mode, groups, stages.Count > 0, hintAfterIdleSeconds, issues);
            return issues;
        }

        private static List<Task> CollectTasks(string listName, IReadOnlyList<Task> tasks, HashSet<Task> listed, List<SetupIssue> issues)
        {
            var collected = new List<Task>();
            for (var index = 0; index < tasks.Count; index++)
            {
                var task = tasks[index];
                if (task == null)
                {
                    issues.Add(new SetupIssue($"{listName} entry {index} is empty."));
                }
                else if (!listed.Add(task))
                {
                    issues.Add(new SetupIssue($"Task '{task.name}' is listed more than once ({listName})."));
                }
                else
                {
                    collected.Add(task);
                }
            }

            return collected;
        }

        private static void CheckStages(IReadOnlyList<TaskStage> stages, List<List<Task>> groups, HashSet<Task> listed, List<SetupIssue> issues)
        {
            for (var index = 0; index < stages.Count; index++)
            {
                var stage = stages[index];
                if (stage == null)
                {
                    issues.Add(new SetupIssue($"Stages entry {index} is empty."));
                    continue;
                }

                if (stage.Tasks.Count == 0)
                {
                    issues.Add(new SetupIssue($"Stage '{stage.StageName}' has no tasks."));
                }

                groups.Add(CollectTasks($"Stage '{stage.StageName}'", new List<Task>(stage.Tasks), listed, issues));
            }
        }

        private static void CheckAudio(string listName, bool hasClips, bool hasAudioSource, List<SetupIssue> issues)
        {
            if (hasClips && !hasAudioSource)
            {
                issues.Add(new SetupIssue($"{listName} has clips but no Audio Source is assigned, so it will not play."));
            }
        }

        private static void CheckTasks(GuidanceMode mode, List<List<Task>> groups, bool hasStages, float hintAfterIdleSeconds, List<SetupIssue> issues)
        {
            var unguided = mode == GuidanceMode.Unguided;
            var hintable = 0;
            foreach (var group in groups)
            {
                foreach (var task in group)
                {
                    if (unguided && task is IGuidanceOnlyTask)
                    {
                        continue;
                    }

                    if (unguided && task is IHintable)
                    {
                        hintable++;
                    }

                    CheckTask(task, unguided, issues);
                }

                if (unguided)
                {
                    CheckGuidanceOnly(group, hasStages, issues);
                }
            }

            if (unguided && hintAfterIdleSeconds > 0f && hintable == 0)
            {
                issues.Add(new SetupIssue("Hint After Idle Seconds is set but no task can give a hint."));
            }
        }

        private static void CheckTask(Task task, bool unguided, List<SetupIssue> issues)
        {
            if (unguided && !(task is IUnguidedTask))
            {
                issues.Add(new SetupIssue($"Task '{task.name}' does not implement IUnguidedTask, so unguided mode starts it as it is started when guided."));
            }

            if (!(task is GuidedTask guided))
            {
                return;
            }

            foreach (var issue in guided.GetSetupIssues(unguided))
            {
                issues.Add(new SetupIssue($"Task '{task.name}': {issue.Message}"));
            }

            if (unguided)
            {
                CheckEnableSubtasks(guided, issues);
            }
        }

        private static void CheckGuidanceOnly(List<Task> tasks, bool hasStages, List<SetupIssue> issues)
        {
            if (tasks.Count == 0)
            {
                return;
            }

            foreach (var task in tasks)
            {
                if (!(task is IGuidanceOnlyTask))
                {
                    return;
                }
            }

            issues.Add(new SetupIssue(hasStages
                ? "A stage only has tasks that give guidance, so unguided mode has nothing to run in it."
                : "Every task only gives guidance, so unguided mode has nothing to run."));
        }

        private static void CheckEnableSubtasks(GuidedTask task, List<SetupIssue> issues)
        {
            foreach (var subtask in task.SubTasks)
            {
                if (subtask is EnableTask && !subtask.IsSetup)
                {
                    issues.Add(new SetupIssue($"Task '{task.name}': subtask '{subtask.name}' turns an object on or off but is not marked Is Setup, so unguided mode only runs it on a hint and the object stays as it is."));
                }
            }
        }
    }
}
