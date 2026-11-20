using System.Collections.Generic;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class UnguidedStepTests
    {
        private GameObject _host;
        private RecordingTask _first;
        private RecordingTask _second;
        private int _finishes;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
            _first = _host.AddComponent<RecordingTask>();
            _second = _host.AddComponent<RecordingTask>();
            _finishes = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void Entering_the_step_starts_every_task_at_once_without_guidance()
        {
            var step = EnterStep();

            Assert.That(_first.UnguidedStartCount, Is.EqualTo(1));
            Assert.That(_second.UnguidedStartCount, Is.EqualTo(1));
            Assert.That(_first.GuidedStartCount, Is.EqualTo(0));
            Assert.That(step.OpenCount, Is.EqualTo(2));
        }

        [Test]
        public void Tasks_that_only_give_guidance_are_left_out()
        {
            var guidance = _host.AddComponent<GuidanceOnlyTask>();
            var step = new UnguidedStep("free", new Task[] { _first, guidance }, null);

            step.Enter();

            Assert.That(step.Tasks, Is.EqualTo(new List<Task> { _first }));
            Assert.That(guidance.StartCount, Is.EqualTo(0));
        }

        [Test]
        public void A_task_without_the_unguided_capability_is_started_normally()
        {
            var plain = _host.AddComponent<PlainTask>();
            var step = new UnguidedStep("free", new Task[] { plain }, null);

            step.Enter();

            Assert.That(plain.StartCount, Is.EqualTo(1));
        }

        [Test]
        public void Pausing_and_resuming_a_step_with_a_task_that_cannot_pause_does_nothing_to_it_and_does_not_throw()
        {
            var plain = _host.AddComponent<PlainTask>();
            var step = new UnguidedStep("free", new Task[] { plain, _first }, null);
            step.Enter();

            Assert.DoesNotThrow(() =>
            {
                step.Pause();
                step.Resume();
            });
            Assert.That(_first.PauseCount, Is.EqualTo(1));
        }

        [Test]
        public void Tasks_can_be_finished_in_any_order_and_the_step_finishes_after_the_last_one()
        {
            var step = EnterStep();

            _second.Finish();
            step.Advance(0.1f);
            var finishedAfterOne = _finishes;
            _first.Finish();
            step.Advance(0.1f);

            Assert.That(finishedAfterOne, Is.EqualTo(0));
            Assert.That(_finishes, Is.EqualTo(1));
        }

        [Test]
        public void The_completion_order_lists_the_tasks_as_they_were_finished_with_positions_from_one()
        {
            var step = EnterStep();

            _second.Finish();
            _first.Finish();

            Assert.That(step.CompletionOrder, Is.EqualTo(new List<Task> { _second, _first }));
            Assert.That(step.PositionOf(_second), Is.EqualTo(1));
            Assert.That(step.PositionOf(_first), Is.EqualTo(2));
        }

        [Test]
        public void A_task_that_is_not_finished_yet_has_position_zero()
        {
            var step = EnterStep();

            _first.Finish();

            Assert.That(step.PositionOf(_second), Is.EqualTo(0));
            Assert.That(step.CompletedCount, Is.EqualTo(1));
            Assert.That(step.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void Each_finished_task_reports_its_position_and_raises_its_own_complete_event()
        {
            var reports = new List<string>();
            var ownEvents = 0;
            var step = NewStep();
            step.TaskCompleted += (task, position) => reports.Add(position + ":" + (task == _first ? "first" : "second"));
            _second.OnCompleteTask.AddListener(() => ownEvents++);
            step.Enter();

            _second.Finish();
            _first.Finish();

            Assert.That(reports, Is.EqualTo(new List<string> { "1:second", "2:first" }));
            Assert.That(ownEvents, Is.EqualTo(1));
        }

        [Test]
        public void Finishing_in_the_listed_order_reports_in_order_once_at_the_end()
        {
            var reports = new List<bool>();
            var step = NewStep();
            step.AllTasksCompleted += reports.Add;
            step.Enter();

            _first.Finish();
            var earlyReports = reports.Count;
            _second.Finish();

            Assert.That(earlyReports, Is.EqualTo(0));
            Assert.That(reports, Is.EqualTo(new List<bool> { true }));
            Assert.That(step.WasCompletedInListedOrder, Is.True);
        }

        [Test]
        public void Finishing_against_the_listed_order_reports_out_of_order()
        {
            var reports = new List<bool>();
            var step = NewStep();
            step.AllTasksCompleted += reports.Add;
            step.Enter();

            _second.Finish();
            _first.Finish();

            Assert.That(reports, Is.EqualTo(new List<bool> { false }));
            Assert.That(step.WasCompletedInListedOrder, Is.False);
        }

        [Test]
        public void A_hint_request_shows_the_hint_of_the_first_open_task()
        {
            var step = EnterStep();

            var shown = step.RequestHint();

            Assert.That(shown, Is.True);
            Assert.That(_first.HintShowCount, Is.EqualTo(1));
            Assert.That(_second.HintShowCount, Is.EqualTo(0));
            Assert.That(step.HintedTask, Is.SameAs(_first));
        }

        [Test]
        public void After_the_first_task_is_done_a_hint_request_goes_to_the_second_task()
        {
            var step = EnterStep();
            _first.Finish();

            step.RequestHint();

            Assert.That(_second.HintShowCount, Is.EqualTo(1));
            Assert.That(_first.HintShowCount, Is.EqualTo(0));
        }

        [Test]
        public void Asking_again_hides_the_hint_and_shows_it_once_more()
        {
            var step = EnterStep();
            step.RequestHint();

            step.RequestHint();

            Assert.That(_first.HintHideCount, Is.EqualTo(1));
            Assert.That(_first.HintShowCount, Is.EqualTo(2));
        }

        [Test]
        public void The_hint_is_taken_down_when_the_hinted_task_finishes()
        {
            var step = EnterStep();
            step.RequestHint();

            _first.Finish();

            Assert.That(step.HintedTask, Is.Null);
            Assert.That(_first.HintHideCount, Is.EqualTo(1));
        }

        [Test]
        public void A_hint_request_before_entering_or_after_everything_is_done_shows_nothing()
        {
            var step = NewStep();
            var beforeEntering = step.RequestHint();
            step.Enter();
            _first.Finish();
            _second.Finish();

            var afterCompleting = step.RequestHint();

            Assert.That(beforeEntering, Is.False);
            Assert.That(afterCompleting, Is.False);
            Assert.That(_first.HintShowCount + _second.HintShowCount, Is.EqualTo(0));
        }

        [Test]
        public void A_hint_is_not_shown_while_a_task_is_still_playing_its_feedback()
        {
            var step = EnterStep();
            _second.Settling = true;

            var shown = step.RequestHint();

            Assert.That(shown, Is.False);
            Assert.That(_first.HintShowCount, Is.EqualTo(0));
        }

        [Test]
        public void Without_an_idle_time_no_hint_appears_by_itself()
        {
            var step = EnterStep();

            step.Advance(1000f);

            Assert.That(_first.HintShowCount, Is.EqualTo(0));
        }

        [Test]
        public void With_an_idle_time_a_hint_appears_after_that_long_without_progress()
        {
            var step = NewStep(hintAfterIdleSeconds: 10f);
            step.Enter();

            step.Advance(9f);
            var early = _first.HintShowCount;
            step.Advance(2f);

            Assert.That(early, Is.EqualTo(0));
            Assert.That(_first.HintShowCount, Is.EqualTo(1));
        }

        [Test]
        public void Finishing_a_task_restarts_the_idle_time()
        {
            var step = NewStep(hintAfterIdleSeconds: 10f);
            step.Enter();
            step.Advance(8f);
            _first.Finish();

            step.Advance(8f);
            var whileBusy = _second.HintShowCount;
            step.Advance(3f);

            Assert.That(whileBusy, Is.EqualTo(0));
            Assert.That(_second.HintShowCount, Is.EqualTo(1));
        }

        [Test]
        public void The_idle_hint_is_not_repeated_for_a_task_that_is_already_hinted()
        {
            var step = NewStep(hintAfterIdleSeconds: 5f);
            step.Enter();

            step.Advance(6f);
            step.Advance(6f);
            step.Advance(6f);

            Assert.That(_first.HintShowCount, Is.EqualTo(1));
        }

        [Test]
        public void While_the_step_is_paused_no_hint_comes_and_the_open_tasks_are_paused()
        {
            var step = NewStep(hintAfterIdleSeconds: 5f);
            step.Enter();
            _first.Finish();

            step.Pause();
            step.Advance(20f);
            var requested = step.RequestHint();
            step.Resume();

            Assert.That(_second.HintShowCount, Is.EqualTo(0));
            Assert.That(requested, Is.False);
            Assert.That(_second.PauseCount, Is.EqualTo(1));
            Assert.That(_second.ResumeCount, Is.EqualTo(1));
            Assert.That(_first.PauseCount, Is.EqualTo(0));
        }

        [Test]
        public void A_task_that_is_complete_as_soon_as_it_starts_counts_and_the_step_waits_for_the_rest()
        {
            _first.FinishWhenStarted = true;

            var step = EnterStep();

            Assert.That(step.CompletedCount, Is.EqualTo(1));
            Assert.That(step.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void A_step_whose_tasks_all_complete_as_they_start_finishes_on_the_next_advance()
        {
            _first.FinishWhenStarted = true;
            _second.FinishWhenStarted = true;
            var step = EnterStep();

            step.Advance(0.1f);

            Assert.That(_finishes, Is.EqualTo(1));
        }

        [Test]
        public void A_step_without_tasks_finishes_on_the_next_advance()
        {
            var step = new UnguidedStep("empty", new Task[0], null);
            step.Finished += () => _finishes++;
            step.Enter();

            step.Advance(0.1f);

            Assert.That(_finishes, Is.EqualTo(1));
        }

        [Test]
        public void The_step_waits_while_a_task_is_still_settling()
        {
            var step = EnterStep();
            _first.Finish();
            _second.Finish();
            _second.Settling = true;

            step.Advance(0.1f);
            var whileSettling = _finishes;
            _second.Settling = false;
            step.Advance(0.1f);

            Assert.That(whileSettling, Is.EqualTo(0));
            Assert.That(_finishes, Is.EqualTo(1));
        }

        [Test]
        public void Skipping_the_step_skips_every_open_task_and_leaves_them_out_of_the_order()
        {
            var step = EnterStep();
            _first.Finish();

            step.Exit(ExitReason.Skipped);

            Assert.That(_second.IsSkip, Is.True);
            Assert.That(_first.IsSkip, Is.False);
            Assert.That(step.CompletionOrder, Is.EqualTo(new List<Task> { _first }));
        }

        [Test]
        public void Skipping_the_step_raises_the_complete_event_of_the_tasks_it_skipped()
        {
            var raised = 0;
            _second.OnCompleteTask.AddListener(() => raised++);
            var step = EnterStep();
            _first.Finish();

            step.Exit(ExitReason.Skipped);

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void A_listener_that_cancels_the_step_while_tasks_are_being_acknowledged_stops_further_acknowledgements()
        {
            var reports = new List<bool>();
            var step = NewStep();
            step.AllTasksCompleted += reports.Add;
            _first.FinishWhenStarted = true;
            _second.FinishWhenStarted = true;
            _first.OnCompleteTask.AddListener(() => step.Exit(ExitReason.Cancelled));

            step.Enter();
            step.Advance(0.1f);

            Assert.That(step.CompletedCount, Is.EqualTo(1));
            Assert.That(reports, Is.Empty);
            Assert.That(_finishes, Is.EqualTo(0));
        }

        [Test]
        public void A_task_without_a_hint_of_its_own_is_passed_over_when_a_hint_is_requested()
        {
            var plain = _host.AddComponent<PlainTask>();
            var step = new UnguidedStep("free", new Task[] { plain, _second }, null);
            step.Enter();

            var shown = step.RequestHint();

            Assert.That(shown, Is.True);
            Assert.That(_second.HintShowCount, Is.EqualTo(1));
        }

        [Test]
        public void A_hint_request_when_no_open_task_has_a_hint_shows_nothing()
        {
            var plainOne = _host.AddComponent<PlainTask>();
            var plainTwo = _host.AddComponent<PlainTask>();
            var step = new UnguidedStep("free", new Task[] { plainOne, plainTwo }, null);
            step.Enter();

            Assert.That(step.RequestHint(), Is.False);
        }

        [Test]
        public void Cancelling_the_step_cancels_the_open_tasks_and_takes_the_hint_down()
        {
            var step = EnterStep();
            _first.Finish();
            step.RequestHint();

            step.Exit(ExitReason.Cancelled);

            Assert.That(_second.CancelCount, Is.EqualTo(1));
            Assert.That(_first.CancelCount, Is.EqualTo(0));
            Assert.That(_second.HintHideCount, Is.EqualTo(1));
            Assert.That(step.HintedTask, Is.Null);
        }

        [Test]
        public void A_task_finishing_after_the_step_was_cancelled_is_ignored()
        {
            var step = EnterStep();
            step.Exit(ExitReason.Cancelled);

            _first.Finish();
            _second.Finish();
            step.Advance(0.1f);

            Assert.That(_finishes, Is.EqualTo(0));
            Assert.That(step.CompletedCount, Is.EqualTo(0));
        }

        [Test]
        public void Entering_the_step_again_forgets_the_previous_order()
        {
            var step = EnterStep();
            _second.Finish();
            _first.Finish();
            step.Exit(ExitReason.Completed);

            step.Enter();

            Assert.That(step.CompletionOrder, Is.Empty);
            Assert.That(step.OpenCount, Is.EqualTo(2));
        }

        private UnguidedStep EnterStep()
        {
            var step = NewStep();
            step.Enter();
            return step;
        }

        private UnguidedStep NewStep(float hintAfterIdleSeconds = 0f)
        {
            var step = new UnguidedStep("free", new Task[] { _first, _second }, null, hintAfterIdleSeconds);
            step.Finished += () => _finishes++;
            return step;
        }
    }
}
