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
            var every = new List<Task>();
            CheckTaskList("Task List", tasks, every, issues);
            CheckStages(stages, every, issues);
            if (tasks.Count == 0 && stages.Count == 0)
            {
                issues.Add(new SetupIssue("The task list is empty, so the sequence completes at once."));
            }

            CheckAudio("Intro Audio", introHasClips, hasAudioSource, issues);
            CheckAudio("Complete Audio", completeHasClips, hasAudioSource, issues);
            if (mode == GuidanceMode.Unguided)
            {
                CheckUnguided(every, hintAfterIdleSeconds, issues);
            }

            return issues;
        }

        private static void CheckTaskList(string listName, IReadOnlyList<Task> tasks, List<Task> every, List<SetupIssue> issues)
        {
            var seen = new HashSet<Task>();
            for (var index = 0; index < tasks.Count; index++)
            {
                var task = tasks[index];
                if (task == null)
                {
                    issues.Add(new SetupIssue($"{listName} entry {index} is empty."));
                }
                else if (!seen.Add(task))
                {
                    issues.Add(new SetupIssue($"Task '{task.name}' is listed more than once in {listName}."));
                }
                else
                {
                    every.Add(task);
                }
            }
        }

        private static void CheckStages(IReadOnlyList<TaskStage> stages, List<Task> every, List<SetupIssue> issues)
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

                var tasks = new List<Task>(stage.Tasks);
                CheckTaskList($"Stage '{stage.StageName}'", tasks, every, issues);
            }
        }

        private static void CheckAudio(string listName, bool hasClips, bool hasAudioSource, List<SetupIssue> issues)
        {
            if (hasClips && !hasAudioSource)
            {
                issues.Add(new SetupIssue($"{listName} has clips but no Audio Source is assigned, so it will not play."));
            }
        }

        private static void CheckUnguided(List<Task> tasks, float hintAfterIdleSeconds, List<SetupIssue> issues)
        {
            var guidanceOnly = 0;
            var hintable = 0;
            foreach (var task in tasks)
            {
                if (task is IGuidanceOnlyTask)
                {
                    guidanceOnly++;
                    continue;
                }

                if (task is IHintable)
                {
                    hintable++;
                }

                if (!(task is IUnguidedTask))
                {
                    issues.Add(new SetupIssue($"Task '{task.name}' does not implement IUnguidedTask, so unguided mode starts it as it is started when guided."));
                }

                if (task is GuidedTask guided)
                {
                    CheckEnableSubtasks(guided, issues);
                }
            }

            if (tasks.Count > 0 && guidanceOnly == tasks.Count)
            {
                issues.Add(new SetupIssue("Every task only gives guidance, so unguided mode has nothing to run."));
            }

            if (hintAfterIdleSeconds > 0f && hintable == 0)
            {
                issues.Add(new SetupIssue("Hint After Idle Seconds is set but no task can give a hint."));
            }
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
