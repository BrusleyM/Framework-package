using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class SequenceRunnerEdgeCaseTests
    {
        private List<string> _journal;
        private RecordingStep _first;
        private RecordingStep _second;
        private RecordingStep _third;

        [SetUp]
        public void SetUp()
        {
            _journal = new List<string>();
            _first = new RecordingStep("first", _journal);
            _second = new RecordingStep("second", _journal);
            _third = new RecordingStep("third", _journal);
        }

        private SequenceRunner RunnerOf(params ISequenceStep[] steps) => new SequenceRunner("edge", steps);

        [Test]
        public void Cancelling_from_a_step_exited_handler_stops_before_the_next_step_is_entered_for_good()
        {
            var runner = RunnerOf(_first, _second);
            runner.StepExited += (step, index, reason) => runner.Cancel();
            runner.Start();

            _first.FinishNow();

            Assert.AreEqual(SequenceState.Cancelled, runner.State);
            Assert.AreEqual(ExitReason.Cancelled, _second.LastExitReason);
            Assert.IsNull(runner.CurrentStep);
        }

        [Test]
        public void Restarting_from_a_step_exited_handler_enters_each_step_once_per_pass()
        {
            var restarted = false;
            var runner = RunnerOf(_first, _second);
            runner.StepExited += (step, index, reason) =>
            {
                if (!restarted && reason == ExitReason.Completed)
                {
                    restarted = true;
                    runner.Restart();
                }
            };
            runner.Start();

            _first.FinishNow();

            Assert.AreEqual(
                new[] { "enter first", "exit first Completed", "enter second", "exit second Restarted", "enter first" },
                _journal);
            Assert.AreEqual(0, runner.CurrentIndex);
        }

        [Test]
        public void Skipping_from_a_step_entered_handler_never_exits_a_step_that_was_not_entered()
        {
            var skipped = false;
            var runner = RunnerOf(_first, _second);
            runner.StepEntered += (step, index) =>
            {
                if (!skipped)
                {
                    skipped = true;
                    runner.SkipCurrentStep();
                }
            };

            runner.Start();

            Assert.AreEqual(new[] { "enter first", "exit first Skipped", "enter second" }, _journal);
        }

        [Test]
        public void A_throwing_exit_while_cancelling_leaves_the_sequence_faulted_not_cancelled()
        {
            var cancelledRaised = false;
            var faultedRaised = false;
            var runner = RunnerOf(new ThrowingStep("stubborn", false, true));
            runner.Cancelled += () => cancelledRaised = true;
            runner.Faulted += exception => faultedRaised = true;
            runner.Start();

            runner.Cancel();

            Assert.AreEqual(SequenceState.Faulted, runner.State);
            Assert.IsTrue(faultedRaised);
            Assert.IsFalse(cancelledRaised);
        }

        [Test]
        public void A_throwing_exit_while_going_back_leaves_the_sequence_faulted_without_entering_another_step()
        {
            var runner = RunnerOf(_first, new ThrowingStep("stubborn", false, true));
            runner.Start();
            _first.FinishNow();

            runner.GoBack();

            Assert.AreEqual(SequenceState.Faulted, runner.State);
            Assert.IsNull(runner.CurrentStep);
            Assert.AreEqual(1, _journal.FindAll(entry => entry == "enter first").Count);
        }

        [Test]
        public void A_faulting_step_is_reported_as_exited_so_entries_and_exits_balance()
        {
            var exits = new List<ExitReason>();
            var runner = RunnerOf(new ThrowingStep("broken", true));
            runner.StepExited += (step, index, reason) => exits.Add(reason);

            runner.Start();

            Assert.AreEqual(new[] { ExitReason.Cancelled }, exits);
        }

        [Test]
        public void A_handler_that_throws_is_rethrown_to_the_caller_after_the_sequence_stays_consistent()
        {
            var runner = RunnerOf(_first, _second);
            runner.StepEntered += (step, index) => throw new InvalidOperationException("handler broke");

            Assert.Throws<InvalidOperationException>(() => runner.Start());
            Assert.AreEqual(SequenceState.Running, runner.State);
            Assert.AreSame(_first, runner.CurrentStep);
        }

        [Test]
        public void A_null_step_is_rejected_when_the_runner_is_created()
        {
            Assert.Throws<ArgumentException>(() => RunnerOf(_first, null));
        }

        [Test]
        public void The_builder_rejects_a_null_step()
        {
            Assert.Throws<ArgumentNullException>(() => Sequence.Named("bad").Then(null));
        }

        [Test]
        public void A_very_long_chain_of_instant_steps_completes_without_deep_recursion()
        {
            var steps = new List<ISequenceStep>();
            for (var count = 0; count < 20000; count++)
            {
                steps.Add(new InstantStep("tick", () => { }));
            }
            var runner = new SequenceRunner("long", steps);

            runner.Start();

            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void Skipping_while_paused_enters_the_next_step_but_keeps_the_sequence_paused()
        {
            var runner = RunnerOf(_first, _second);
            runner.Start();
            runner.Pause();

            runner.SkipCurrentStep();

            Assert.AreEqual(SequenceState.Paused, runner.State);
            Assert.AreEqual(1, runner.CurrentIndex);
        }

        [Test]
        public void Pausing_while_a_step_enters_defers_its_instant_finish_until_resume()
        {
            var runner = RunnerOf(new InstantStep("instant", () => { }), _second);
            runner.StepEntered += (step, index) => runner.Pause();

            runner.Start();
            Assert.AreEqual(0, runner.CurrentIndex);

            runner.Resume();
            Assert.AreEqual(1, runner.CurrentIndex);
        }

        [Test]
        public void A_reminding_step_does_not_remind_on_the_tick_its_inner_step_finishes()
        {
            var reminders = 0;
            var runner = RunnerOf(new RemindingStep(new DelayStep("short", 0.5f), 0.5f, () => reminders++));
            runner.Start();

            runner.Advance(0.5f);

            Assert.AreEqual(SequenceState.Completed, runner.State);
            Assert.AreEqual(0, reminders);
        }

        [Test]
        public void A_reminding_step_needs_an_inner_step()
        {
            Assert.Throws<ArgumentNullException>(() => new RemindingStep(null, 1f, () => { }));
        }

        [Test]
        public void Cancelling_a_parallel_step_still_exits_every_child_when_one_exit_throws()
        {
            var stubborn = new ThrowingStep("stubborn", false, true);
            var runner = Sequence.Named("both").Together("pair", stubborn, _second).Build();
            runner.Start();

            runner.Cancel();

            Assert.AreEqual(ExitReason.Cancelled, _second.LastExitReason);
        }

        [Test]
        public void A_parallel_step_without_children_finishes_immediately()
        {
            var runner = Sequence.Named("nobody").Together("empty").Build();

            runner.Start();

            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void A_parallel_child_that_finishes_while_entering_does_not_finish_the_group_early()
        {
            var instant = new RecordingStep("instant", _journal, finishesWhenEntered: true);
            var runner = Sequence.Named("group").Together("pair", instant, _second).Build();

            runner.Start();
            Assert.AreEqual(SequenceState.Running, runner.State);

            _second.FinishNow();
            Assert.AreEqual(SequenceState.Completed, runner.State);
        }
    }
}
