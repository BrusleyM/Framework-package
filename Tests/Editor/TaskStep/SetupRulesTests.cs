using System.Collections.Generic;
using System.Linq;
using Framework.Audio;
using Framework.CheckPoints;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class SetupRulesTests
    {
        private GameObject _host;
        private SequentialTask _sequence;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
            _sequence = _host.AddComponent<SequentialTask>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void An_empty_task_list_is_reported()
        {
            Assert.That(Messages(_sequence), Has.Some.Contain("task list is empty"));
        }

        [Test]
        public void A_guided_list_of_ordinary_tasks_has_no_issues()
        {
            _sequence.AddTask(_host.AddComponent<PlainTask>());

            Assert.That(Messages(_sequence), Is.Empty);
        }

        [Test]
        public void An_empty_entry_in_the_task_list_is_reported_with_its_position()
        {
            _sequence.AddTask(_host.AddComponent<PlainTask>());
            _sequence.AddTask(null);

            Assert.That(Messages(_sequence), Has.Some.Contain("Task List entry 1 is empty"));
        }

        [Test]
        public void A_task_listed_twice_is_reported()
        {
            var task = _host.AddComponent<PlainTask>();
            _sequence.AddTask(task);
            _sequence.AddTask(task);

            Assert.That(Messages(_sequence), Has.Some.Contain("listed more than once"));
        }

        [Test]
        public void A_stage_without_tasks_is_reported()
        {
            var stageObject = new GameObject("stage one");
            stageObject.transform.SetParent(_host.transform);
            _sequence.AddStage(stageObject.AddComponent<TaskStage>());

            Assert.That(Messages(_sequence), Has.Some.Contain("Stage 'stage one' has no tasks"));
        }

        [Test]
        public void Intro_audio_without_an_audio_source_is_reported_and_with_one_is_not()
        {
            _sequence.AddTask(_host.AddComponent<PlainTask>());
            PrivateMembers.SetField(_sequence, "_introAudio", new List<MyClip> { Clip() });
            var withoutSource = Messages(_sequence);

            PrivateMembers.SetField(_sequence, "_audioSource", _host.AddComponent<AudioSource>());

            Assert.That(withoutSource, Has.Some.Contain("Intro Audio has clips but no Audio Source"));
            Assert.That(Messages(_sequence), Is.Empty);
        }

        [Test]
        public void Complete_audio_without_an_audio_source_is_reported()
        {
            _sequence.AddTask(_host.AddComponent<PlainTask>());
            PrivateMembers.SetField(_sequence, "_completeAudio", new List<MyClip> { Clip() });

            Assert.That(Messages(_sequence), Has.Some.Contain("Complete Audio has clips but no Audio Source"));
        }

        [Test]
        public void Unguided_mode_with_only_guidance_tasks_has_nothing_to_run()
        {
            _sequence.AddTask(_host.AddComponent<GuidanceTask>());
            _sequence.SetMode(GuidanceMode.Unguided);

            Assert.That(Messages(_sequence), Has.Some.Contain("nothing to run"));
        }

        [Test]
        public void Unguided_mode_reports_a_task_that_cannot_run_unguided_but_guided_mode_does_not()
        {
            _sequence.AddTask(_host.AddComponent<PlainTask>());
            var guided = Messages(_sequence);

            _sequence.SetMode(GuidanceMode.Unguided);

            Assert.That(guided, Is.Empty);
            Assert.That(Messages(_sequence), Has.Some.Contain("does not implement IUnguidedTask"));
        }

        [Test]
        public void An_enable_subtask_that_is_not_setup_is_reported_in_unguided_mode_and_not_once_it_is_setup()
        {
            var task = _host.AddComponent<CapableTask>();
            var enable = _host.AddComponent<EnableTask>();
            PrivateMembers.SetField(task, "_subTasks", new GameObjectTask[] { enable });
            _sequence.AddTask(task);
            _sequence.SetMode(GuidanceMode.Unguided);
            var before = Messages(_sequence);

            PrivateMembers.SetField(enable, "_isSetup", true);

            Assert.That(before, Has.Some.Contain("not marked Is Setup"));
            Assert.That(Messages(_sequence), Has.None.Contain("not marked Is Setup"));
        }

        [Test]
        public void A_hint_after_idle_time_without_any_hintable_task_is_reported()
        {
            _sequence.AddTask(_host.AddComponent<UnguidedOnlyTask>());
            _sequence.SetMode(GuidanceMode.Unguided);
            PrivateMembers.SetField(_sequence, "_hintAfterIdleSeconds", 10f);

            Assert.That(Messages(_sequence), Has.Some.Contain("no task can give a hint"));
        }

        [Test]
        public void A_complete_unguided_setup_has_no_issues()
        {
            _sequence.AddTask(_host.AddComponent<CapableTask>());
            _sequence.SetMode(GuidanceMode.Unguided);
            PrivateMembers.SetField(_sequence, "_hintAfterIdleSeconds", 10f);

            Assert.That(Messages(_sequence), Has.None.Contain("no task can give a hint"));
            Assert.That(Messages(_sequence), Has.None.Contain("IUnguidedTask"));
        }

        [Test]
        public void An_empty_subtask_entry_is_reported_by_a_guided_task()
        {
            var task = _host.AddComponent<AnswerTask>();
            PrivateMembers.SetField(task, "_subTasks", new GameObjectTask[] { null });

            Assert.That(Messages(task), Has.Some.Contain("Sub Tasks entry 0 is empty"));
        }

        [Test]
        public void A_reminder_without_instruction_audio_is_reported()
        {
            var task = _host.AddComponent<AnswerTask>();
            PrivateMembers.SetField(task, "_reminderInterval", 5f);

            Assert.That(Messages(task), Has.Some.Contain("Reminder Interval is set but there is no Instruction Audio"));
        }

        [Test]
        public void A_hint_that_would_show_nothing_is_reported_and_a_guidance_subtask_or_instruction_fixes_it()
        {
            var task = _host.AddComponent<AnswerTask>();
            var setup = _host.AddComponent<MarkerSubtask>();
            PrivateMembers.SetField(setup, "_isSetup", true);
            PrivateMembers.SetField(task, "_subTasks", new GameObjectTask[] { setup });
            var withOnlySetup = Messages(task);

            PrivateMembers.SetField(task, "_subTasks", new GameObjectTask[] { setup, _host.AddComponent<MarkerSubtask>() });
            var withGuidance = Messages(task);
            PrivateMembers.SetField(task, "_subTasks", new GameObjectTask[0]);
            PrivateMembers.SetField(task, "_instructionalAudio", Clip());

            Assert.That(withOnlySetup, Has.Some.Contain("A hint would show and play nothing"));
            Assert.That(withGuidance, Has.None.Contain("A hint would show and play nothing"));
            Assert.That(Messages(task), Has.None.Contain("A hint would show and play nothing"));
        }

        [Test]
        public void An_action_task_without_check_points_is_reported_and_with_one_is_not()
        {
            var action = _host.AddComponent<ActionTask>();
            PrivateMembers.SetField(action, "_checkPoints", new CheckPointsParent());
            PrivateMembers.SetField(action, "_instructionalAudio", Clip());
            var without = Messages(action);

            action.CheckPointsParent.CheckPoints.Add(_host.AddComponent<CheckPoint>());

            Assert.That(without, Has.Some.Contain("No check points are assigned"));
            Assert.That(Messages(action), Has.None.Contain("No check points are assigned"));
        }

        [Test]
        public void An_action_task_that_was_never_given_a_check_points_object_is_reported_too()
        {
            var action = _host.AddComponent<ActionTask>();

            Assert.That(Messages(action), Has.Some.Contain("No check points are assigned"));
        }

        [Test]
        public void A_highlight_subtask_marked_setup_is_reported_because_it_would_show_without_a_hint()
        {
            var action = _host.AddComponent<ActionTask>();
            var highlight = _host.AddComponent<SimultaneousHighlightTask>();
            PrivateMembers.SetField(highlight, "_isSetup", true);
            PrivateMembers.SetField(action, "_subTasks", new GameObjectTask[] { highlight });

            Assert.That(Messages(action), Has.Some.Contain("is a highlight marked Is Setup"));
        }

        private static List<string> Messages(SequentialTask sequence)
        {
            return sequence.GetSetupIssues().Select(issue => issue.Message).ToList();
        }

        private static List<string> Messages(GuidedTask task)
        {
            return task.GetSetupIssues().Select(issue => issue.Message).ToList();
        }

        private static MyClip Clip()
        {
            return new MyClip { name = "clip", clip = AudioClip.Create("clip", 100, 1, 44100, false) };
        }

        private sealed class PlainTask : Task
        {
            public override void StartTask()
            {
            }
        }

        private sealed class GuidanceTask : Task, IGuidanceOnlyTask
        {
            public override void StartTask()
            {
            }
        }

        private sealed class UnguidedOnlyTask : Task, IUnguidedTask
        {
            public override void StartTask()
            {
            }

            public void StartTaskUnguided()
            {
            }
        }

        private sealed class CapableTask : GuidedTask
        {
            protected override void BeginCompletionTracking()
            {
            }

            protected override void EndCompletionTracking()
            {
            }

            protected override void ResetCompletionTracking()
            {
            }
        }

        private sealed class AnswerTask : GuidedTask
        {
            protected override void BeginCompletionTracking()
            {
            }

            protected override void EndCompletionTracking()
            {
            }

            protected override void ResetCompletionTracking()
            {
            }
        }

        private sealed class MarkerSubtask : GameObjectTask
        {
            public override void Execute()
            {
            }
        }
    }
}
