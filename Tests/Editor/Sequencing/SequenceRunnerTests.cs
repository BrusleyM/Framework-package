using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class SequenceRunnerTests
    {
        private List<string> _journal;
        private RecordingStep _first;
        private RecordingStep _second;
        private RecordingStep _third;
        private SequenceRunner _runner;

        [SetUp]
        public void SetUp()
        {
            _journal = new List<string>();
            _first = new RecordingStep("first", _journal);
            _second = new RecordingStep("second", _journal);
            _third = new RecordingStep("third", _journal);
            _runner = new SequenceRunner("tutorial", new ISequenceStep[] { _first, _second, _third });
        }

        [Test]
        public void A_new_runner_is_idle_and_has_not_entered_any_step()
        {
            Assert.AreEqual(SequenceState.Idle, _runner.State);
            Assert.AreEqual(-1, _runner.CurrentIndex);
            Assert.IsEmpty(_journal);
        }

        [Test]
        public void Starting_enters_only_the_first_step()
        {
            _runner.Start();

            Assert.AreEqual(SequenceState.Running, _runner.State);
            Assert.AreEqual(new[] { "enter first" }, _journal);
        }

        [Test]
        public void Starting_twice_does_not_enter_the_first_step_again()
        {
            _runner.Start();
            _runner.Start();

            Assert.AreEqual(new[] { "enter first" }, _journal);
        }

        [Test]
        public void A_finished_step_hands_over_to_the_next_step()
        {
            _runner.Start();

            _first.FinishNow();

            Assert.AreEqual(new[] { "enter first", "exit first Completed", "enter second" }, _journal);
            Assert.AreEqual(1, _runner.CurrentIndex);
        }

        [Test]
        public void Finishing_the_last_step_completes_the_sequence_once()
        {
            var completions = 0;
            _runner.Completed += () => completions++;
            _runner.Start();

            _first.FinishNow();
            _second.FinishNow();
            _third.FinishNow();
            _third.FinishNow();

            Assert.AreEqual(SequenceState.Completed, _runner.State);
            Assert.AreEqual(1, completions);
            Assert.IsNull(_runner.CurrentStep);
        }

        [Test]
        public void An_empty_sequence_completes_as_soon_as_it_starts()
        {
            var runner = new SequenceRunner("empty", new ISequenceStep[0]);

            runner.Start();

            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void A_step_that_finishes_while_entering_does_not_stall_the_sequence()
        {
            var instant = new RecordingStep("instant", _journal, finishesWhenEntered: true);
            var runner = new SequenceRunner("instant first", new ISequenceStep[] { instant, _second });

            runner.Start();

            Assert.AreEqual(new[] { "enter instant", "exit instant Completed", "enter second" }, _journal);
        }

        [Test]
        public void A_chain_of_instant_steps_runs_to_completion_in_one_call()
        {
            var steps = new ISequenceStep[]
            {
                new RecordingStep("a", _journal, true),
                new RecordingStep("b", _journal, true),
                new RecordingStep("c", _journal, true)
            };
            var runner = new SequenceRunner("instants", steps);

            runner.Start();

            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void Advancing_only_reaches_the_active_step()
        {
            _runner.Start();

            _runner.Advance(0.5f);
            _runner.Advance(0.25f);

            Assert.AreEqual(0.75f, _first.AdvancedSeconds, 0.0001f);
            Assert.AreEqual(0f, _second.AdvancedSeconds);
        }

        [Test]
        public void Skipping_exits_the_current_step_as_skipped_and_enters_the_next()
        {
            _runner.Start();

            _runner.SkipCurrentStep();

            Assert.AreEqual(ExitReason.Skipped, _first.LastExitReason);
            Assert.AreEqual(1, _runner.CurrentIndex);
        }

        [Test]
        public void Skipping_the_last_step_completes_the_sequence()
        {
            _runner.Start();
            _runner.SkipCurrentStep();
            _runner.SkipCurrentStep();
            _runner.SkipCurrentStep();

            Assert.AreEqual(SequenceState.Completed, _runner.State);
        }

        [Test]
        public void Going_back_exits_the_current_step_as_rewound_and_reenters_the_previous_one()
        {
            _runner.Start();
            _first.FinishNow();

            _runner.GoBack();

            Assert.AreEqual(ExitReason.Rewound, _second.LastExitReason);
            Assert.AreEqual(0, _runner.CurrentIndex);
            Assert.AreEqual("enter first", _journal[_journal.Count - 1]);
        }

        [Test]
        public void Going_back_on_the_first_step_reenters_the_first_step()
        {
            _runner.Start();

            _runner.GoBack();

            Assert.AreEqual(new[] { "enter first", "exit first Rewound", "enter first" }, _journal);
        }

        [Test]
        public void Restarting_a_running_sequence_exits_the_active_step_and_begins_again()
        {
            _runner.Start();
            _first.FinishNow();

            _runner.Restart();

            Assert.AreEqual(ExitReason.Restarted, _second.LastExitReason);
            Assert.AreEqual(0, _runner.CurrentIndex);
            Assert.AreEqual(SequenceState.Running, _runner.State);
        }

        [Test]
        public void A_completed_sequence_can_be_restarted()
        {
            _runner.Start();
            _runner.SkipCurrentStep();
            _runner.SkipCurrentStep();
            _runner.SkipCurrentStep();

            _runner.Restart();

            Assert.AreEqual(SequenceState.Running, _runner.State);
            Assert.AreEqual(0, _runner.CurrentIndex);
        }

        [Test]
        public void Cancelling_exits_the_active_step_as_cancelled_and_raises_cancelled_once()
        {
            var cancellations = 0;
            _runner.Cancelled += () => cancellations++;
            _runner.Start();

            _runner.Cancel();
            _runner.Cancel();

            Assert.AreEqual(ExitReason.Cancelled, _first.LastExitReason);
            Assert.AreEqual(SequenceState.Cancelled, _runner.State);
            Assert.AreEqual(1, cancellations);
        }

        [Test]
        public void A_cancelled_sequence_ignores_a_late_finish_from_its_old_step()
        {
            _runner.Start();
            _runner.Cancel();

            _first.FinishNow();

            Assert.AreEqual(SequenceState.Cancelled, _runner.State);
            Assert.AreEqual(-0, _second.AdvancedSeconds);
        }

        [Test]
        public void Pausing_stops_time_from_reaching_the_active_step()
        {
            _runner.Start();
            _runner.Pause();

            _runner.Advance(5f);

            Assert.AreEqual(SequenceState.Paused, _runner.State);
            Assert.AreEqual(0f, _first.AdvancedSeconds);
        }

        [Test]
        public void Resuming_lets_time_reach_the_active_step_again()
        {
            _runner.Start();
            _runner.Pause();
            _runner.Resume();

            _runner.Advance(1f);

            Assert.AreEqual(1f, _first.AdvancedSeconds, 0.0001f);
        }

        [Test]
        public void A_step_finishing_while_paused_hands_over_only_after_resume()
        {
            _runner.Start();
            _runner.Pause();

            _first.FinishNow();
            Assert.AreEqual(0, _runner.CurrentIndex);

            _runner.Resume();
            Assert.AreEqual(1, _runner.CurrentIndex);
        }

        [Test]
        public void Pause_and_resume_outside_their_states_change_nothing()
        {
            _runner.Resume();
            Assert.AreEqual(SequenceState.Idle, _runner.State);

            _runner.Start();
            _runner.Resume();
            Assert.AreEqual(SequenceState.Running, _runner.State);
        }

        [Test]
        public void Step_events_report_every_entry_and_exit_with_its_index_and_reason()
        {
            var events = new List<string>();
            _runner.StepEntered += (step, index) => events.Add($"entered {step.Name} #{index}");
            _runner.StepExited += (step, index, reason) => events.Add($"exited {step.Name} #{index} {reason}");

            _runner.Start();
            _runner.SkipCurrentStep();

            Assert.AreEqual(
                new[] { "entered first #0", "exited first #0 Skipped", "entered second #1" },
                events);
        }

        [Test]
        public void A_step_that_throws_on_entering_faults_the_sequence_and_reports_the_exception()
        {
            Exception reported = null;
            var runner = new SequenceRunner("fragile", new ISequenceStep[] { new ThrowingStep("broken", true), _second });
            runner.Faulted += exception => reported = exception;

            runner.Start();

            Assert.AreEqual(SequenceState.Faulted, runner.State);
            Assert.IsInstanceOf<InvalidOperationException>(reported);
            Assert.IsNull(runner.CurrentStep);
        }

        [Test]
        public void The_runner_copies_its_steps_so_later_changes_to_the_source_list_do_not_leak_in()
        {
            var source = new List<ISequenceStep> { _first };
            var runner = new SequenceRunner("copy", source);

            source.Add(_second);

            Assert.AreEqual(1, runner.StepCount);
        }
    }
}
