using System.Collections.Generic;
using Framework.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class FreeGroupTaskTests
    {
        private GameObject _host;
        private FreeGroupTask _group;
        private HintableTask _first;
        private HintableTask _second;
        private int _groupCompletions;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
            _group = _host.AddComponent<FreeGroupTask>();
            _first = _host.AddComponent<HintableTask>();
            _second = _host.AddComponent<HintableTask>();
            Assign(_group, _first, _second);
            _groupCompletions = 0;
            _group.Completed += () => _groupCompletions++;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void Starting_the_group_starts_every_task_at_once_without_guidance()
        {
            _group.StartTask();

            Assert.That(_first.UnguidedStartCount, Is.EqualTo(1));
            Assert.That(_second.UnguidedStartCount, Is.EqualTo(1));
            Assert.That(_first.GuidedStartCount, Is.EqualTo(0));
            Assert.That(_second.GuidedStartCount, Is.EqualTo(0));
        }

        [Test]
        public void Tasks_can_be_finished_in_any_order_and_the_group_completes_after_the_last()
        {
            _group.StartTask();

            _second.Finish();
            var completedAfterOne = _group.IsComplete;
            _first.Finish();

            Assert.That(completedAfterOne, Is.False);
            Assert.That(_group.IsComplete, Is.True);
            Assert.That(_groupCompletions, Is.EqualTo(1));
        }

        [Test]
        public void The_completed_and_open_counts_follow_the_finished_tasks()
        {
            _group.StartTask();

            _first.Finish();

            Assert.That(_group.CompletedCount, Is.EqualTo(1));
            Assert.That(_group.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void Finishing_a_task_raises_its_own_complete_event_and_the_group_event_with_that_task()
        {
            var reported = new List<Task>();
            var ownEvents = 0;
            _group.OnTaskCompleted.AddListener(reported.Add);
            _first.OnCompleteTask.AddListener(() => ownEvents++);
            _group.StartTask();

            _first.Finish();

            Assert.That(reported, Is.EqualTo(new List<Task> { _first }));
            Assert.That(ownEvents, Is.EqualTo(1));
        }

        [Test]
        public void A_hint_request_shows_the_hint_of_the_first_open_task()
        {
            _group.StartTask();

            var shown = _group.RequestHint();

            Assert.That(shown, Is.True);
            Assert.That(_first.HintShowCount, Is.EqualTo(1));
            Assert.That(_second.HintShowCount, Is.EqualTo(0));
            Assert.That(_group.HintedTask, Is.SameAs(_first));
        }

        [Test]
        public void After_the_first_task_is_done_a_hint_request_goes_to_the_second_task()
        {
            _group.StartTask();
            _first.Finish();

            _group.RequestHint();

            Assert.That(_second.HintShowCount, Is.EqualTo(1));
            Assert.That(_first.HintShowCount, Is.EqualTo(0));
        }

        [Test]
        public void Asking_again_hides_the_hint_and_shows_it_once_more()
        {
            _group.StartTask();
            _group.RequestHint();

            _group.RequestHint();

            Assert.That(_first.HintHideCount, Is.EqualTo(1));
            Assert.That(_first.HintShowCount, Is.EqualTo(2));
        }

        [Test]
        public void The_hint_is_given_up_when_the_hinted_task_finishes()
        {
            _group.StartTask();
            _group.RequestHint();

            _first.Finish();

            Assert.That(_group.HintedTask, Is.Null);
        }

        [Test]
        public void A_hint_request_before_the_group_starts_or_after_it_completes_shows_nothing()
        {
            var beforeStart = _group.RequestHint();
            _group.StartTask();
            _first.Finish();
            _second.Finish();

            var afterComplete = _group.RequestHint();

            Assert.That(beforeStart, Is.False);
            Assert.That(afterComplete, Is.False);
            Assert.That(_first.HintShowCount + _second.HintShowCount, Is.EqualTo(0));
        }

        [Test]
        public void A_task_that_is_complete_as_soon_as_it_starts_counts_and_the_group_waits_for_the_rest()
        {
            _first.FinishWhenStarted = true;

            _group.StartTask();

            Assert.That(_group.CompletedCount, Is.EqualTo(1));
            Assert.That(_group.IsComplete, Is.False);
        }

        [Test]
        public void A_group_whose_tasks_all_complete_as_they_start_completes_at_once()
        {
            _first.FinishWhenStarted = true;
            _second.FinishWhenStarted = true;

            _group.StartTask();

            Assert.That(_group.IsComplete, Is.True);
            Assert.That(_groupCompletions, Is.EqualTo(1));
        }

        [Test]
        public void A_group_without_tasks_completes_at_once()
        {
            Assign(_group);

            _group.StartTask();

            Assert.That(_group.IsComplete, Is.True);
        }

        [Test]
        public void Skipping_the_group_skips_every_open_task_and_completes_once()
        {
            _group.StartTask();
            _first.Finish();

            _group.Skip();

            Assert.That(_second.IsSkip, Is.True);
            Assert.That(_first.IsSkip, Is.False);
            Assert.That(_group.IsSkip, Is.True);
            Assert.That(_groupCompletions, Is.EqualTo(1));
        }

        [Test]
        public void Cancelling_the_group_cancels_the_open_tasks_and_hides_the_hint()
        {
            _group.StartTask();
            _first.Finish();
            _group.RequestHint();

            _group.CancelTask();

            Assert.That(_second.CancelCount, Is.EqualTo(1));
            Assert.That(_first.CancelCount, Is.EqualTo(0));
            Assert.That(_second.HintHideCount, Is.EqualTo(1));
            Assert.That(_group.HintedTask, Is.Null);
        }

        [Test]
        public void A_finished_task_after_the_group_was_cancelled_is_ignored()
        {
            _group.StartTask();
            _group.CancelTask();

            _first.Finish();
            _second.Finish();

            Assert.That(_group.IsComplete, Is.False);
            Assert.That(_groupCompletions, Is.EqualTo(0));
        }

        [Test]
        public void A_group_can_be_prepared_and_started_again_after_completing()
        {
            _group.StartTask();
            _first.Finish();
            _second.Finish();

            _group.Prepare();
            _group.StartTask();
            _first.Finish();
            _second.Finish();

            Assert.That(_groupCompletions, Is.EqualTo(2));
        }

        [Test]
        public void The_group_is_settling_while_any_task_is_settling()
        {
            _group.StartTask();
            _second.Settling = true;

            Assert.That(_group.IsSettling, Is.True);
        }

        [Test]
        public void A_hint_is_not_shown_while_a_task_is_still_playing_its_feedback()
        {
            _group.StartTask();
            _second.Settling = true;

            var shown = _group.RequestHint();

            Assert.That(shown, Is.False);
            Assert.That(_first.HintShowCount, Is.EqualTo(0));
        }

        [Test]
        public void Completing_the_group_directly_cancels_the_open_tasks_and_takes_the_hint_down()
        {
            _group.StartTask();
            _group.RequestHint();

            _group.TaskComplete();

            Assert.That(_first.CancelCount, Is.EqualTo(1));
            Assert.That(_second.CancelCount, Is.EqualTo(1));
            Assert.That(_first.HintHideCount, Is.EqualTo(1));
            Assert.That(_group.HintedTask, Is.Null);
        }

        [Test]
        public void Starting_the_group_again_forgets_the_hint_of_the_previous_run()
        {
            _group.StartTask();
            _group.RequestHint();
            _group.CancelTask();

            _group.Prepare();
            _group.StartTask();
            _group.RequestHint();

            Assert.That(_first.HintShowCount, Is.EqualTo(2));
        }

        [Test]
        public void The_hint_of_a_task_that_finishes_is_hidden()
        {
            _group.StartTask();
            _group.RequestHint();

            _first.Finish();

            Assert.That(_first.HintHideCount, Is.EqualTo(1));
        }

        [Test]
        public void A_group_inside_a_group_passes_the_hint_on_to_its_own_first_open_task()
        {
            var inner = _host.AddComponent<FreeGroupTask>();
            var innerTask = _host.AddComponent<HintableTask>();
            Assign(inner, innerTask);
            var outer = _host.AddComponent<FreeGroupTask>();
            Assign(outer, inner);
            outer.StartTask();

            outer.RequestHint();

            Assert.That(innerTask.HintShowCount, Is.EqualTo(1));
        }

        [Test]
        public void The_completion_order_lists_the_tasks_as_they_were_finished_and_positions_start_at_one()
        {
            _group.StartTask();

            _second.Finish();
            _first.Finish();

            Assert.That(_group.CompletionOrder, Is.EqualTo(new List<Task> { _second, _first }));
            Assert.That(_group.PositionOf(_second), Is.EqualTo(1));
            Assert.That(_group.PositionOf(_first), Is.EqualTo(2));
        }

        [Test]
        public void A_task_that_is_not_finished_yet_has_position_zero()
        {
            _group.StartTask();

            _first.Finish();

            Assert.That(_group.PositionOf(_second), Is.EqualTo(0));
        }

        [Test]
        public void Each_finished_task_reports_its_position_as_it_completes()
        {
            var reports = new List<string>();
            _group.OnTaskCompletedAtPosition.AddListener((task, position) => reports.Add(position + ":" + (task == _first ? "first" : "second")));
            _group.StartTask();

            _second.Finish();
            _first.Finish();

            Assert.That(reports, Is.EqualTo(new List<string> { "1:second", "2:first" }));
        }

        [Test]
        public void Finishing_in_the_listed_order_raises_the_in_order_event_once_at_the_end()
        {
            var inOrder = 0;
            var outOfOrder = 0;
            _group.OnCompletedInListedOrder.AddListener(() => inOrder++);
            _group.OnCompletedOutOfOrder.AddListener(() => outOfOrder++);
            _group.StartTask();

            _first.Finish();
            var raisedEarly = inOrder;
            _second.Finish();

            Assert.That(raisedEarly, Is.EqualTo(0));
            Assert.That(inOrder, Is.EqualTo(1));
            Assert.That(outOfOrder, Is.EqualTo(0));
            Assert.That(_group.WasCompletedInListedOrder, Is.True);
        }

        [Test]
        public void Finishing_against_the_listed_order_raises_the_out_of_order_event()
        {
            var inOrder = 0;
            var outOfOrder = 0;
            _group.OnCompletedInListedOrder.AddListener(() => inOrder++);
            _group.OnCompletedOutOfOrder.AddListener(() => outOfOrder++);
            _group.StartTask();

            _second.Finish();
            _first.Finish();

            Assert.That(inOrder, Is.EqualTo(0));
            Assert.That(outOfOrder, Is.EqualTo(1));
            Assert.That(_group.WasCompletedInListedOrder, Is.False);
        }

        [Test]
        public void Starting_again_clears_the_recorded_order()
        {
            _group.StartTask();
            _second.Finish();
            _first.Finish();

            _group.Prepare();
            _group.StartTask();

            Assert.That(_group.CompletionOrder, Is.Empty);
        }

        [Test]
        public void Skipped_tasks_are_left_out_of_the_order_and_no_order_event_is_raised()
        {
            var raised = 0;
            _group.OnCompletedInListedOrder.AddListener(() => raised++);
            _group.OnCompletedOutOfOrder.AddListener(() => raised++);
            _group.StartTask();
            _first.Finish();

            _group.Skip();

            Assert.That(_group.CompletionOrder, Is.EqualTo(new List<Task> { _first }));
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void Pausing_the_group_pauses_the_open_tasks_and_resuming_resumes_them()
        {
            _group.StartTask();
            _first.Finish();

            _group.PauseTask();
            _group.ResumeTask();

            Assert.That(_second.PauseCount, Is.EqualTo(1));
            Assert.That(_second.ResumeCount, Is.EqualTo(1));
            Assert.That(_first.PauseCount, Is.EqualTo(0));
        }

        [Test]
        public void A_hint_cannot_be_requested_while_the_group_is_paused_and_works_again_after_resuming()
        {
            _group.StartTask();
            _group.PauseTask();

            var whilePaused = _group.RequestHint();
            _group.ResumeTask();
            var afterResume = _group.RequestHint();

            Assert.That(whilePaused, Is.False);
            Assert.That(afterResume, Is.True);
        }

        private static void Assign(FreeGroupTask group, params Task[] tasks)
        {
            var serialized = new SerializedObject(group);
            var list = serialized.FindProperty("_tasks");
            list.arraySize = tasks.Length;
            for (var index = 0; index < tasks.Length; index++)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = tasks[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class HintableTask : Task
        {
            public int GuidedStartCount { get; private set; }
            public int UnguidedStartCount { get; private set; }
            public int CancelCount { get; private set; }
            public int HintShowCount { get; private set; }
            public int HintHideCount { get; private set; }
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }
            public bool FinishWhenStarted { get; set; }
            public bool Settling { get; set; }

            public override bool IsSettling => Settling;

            public void Finish() => TaskComplete();

            public override void StartTask() => GuidedStartCount++;

            public override void StartTaskUnguided()
            {
                UnguidedStartCount++;
                if (FinishWhenStarted)
                {
                    TaskComplete();
                }
            }

            public override void CancelTask() => CancelCount++;

            public override void PauseTask() => PauseCount++;

            public override void ResumeTask() => ResumeCount++;

            public override void ShowHint() => HintShowCount++;

            public override void HideHint() => HintHideCount++;
        }
    }
}
