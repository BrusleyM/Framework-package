using System.Collections.Generic;
using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class BuiltInStepTests
    {
        [Test]
        public void A_delay_step_finishes_once_enough_time_has_been_advanced()
        {
            var runner = Sequence.Named("delay").Wait(1f).Build();
            runner.Start();

            runner.Advance(0.6f);
            Assert.AreEqual(SequenceState.Running, runner.State);

            runner.Advance(0.6f);
            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void A_zero_second_delay_finishes_immediately()
        {
            var runner = Sequence.Named("no wait").Wait(0f).Build();

            runner.Start();

            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void A_condition_step_waits_until_the_condition_is_satisfied()
        {
            var condition = new ManualCondition();
            var runner = Sequence.Named("poke").WaitFor("poke the button", condition).Build();

            runner.Start();
            Assert.IsTrue(condition.IsArmed);
            Assert.AreEqual(SequenceState.Running, runner.State);

            condition.Satisfy();
            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void A_condition_that_is_already_satisfied_lets_the_sequence_move_on()
        {
            var condition = new ManualCondition { SatisfiedImmediately = true };
            var runner = Sequence.Named("already done").WaitFor("grab", condition).Build();

            runner.Start();

            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void A_condition_step_disarms_and_unsubscribes_when_the_sequence_is_cancelled()
        {
            var condition = new ManualCondition();
            var runner = Sequence.Named("cancel").WaitFor("grab", condition).Build();
            runner.Start();

            runner.Cancel();

            Assert.IsFalse(condition.IsArmed);
            Assert.AreEqual(0, condition.SubscriberCount);
        }

        [Test]
        public void Restarting_a_condition_sequence_never_stacks_subscriptions()
        {
            var condition = new ManualCondition();
            var runner = Sequence.Named("restart").WaitFor("grab", condition).Build();
            runner.Start();

            runner.Restart();
            runner.Restart();

            Assert.AreEqual(1, condition.SubscriberCount);
        }

        [Test]
        public void An_instant_step_runs_its_action_and_reports_how_it_exited()
        {
            var journal = new List<string>();
            var step = new InstantStep("show hint", () => journal.Add("shown"), reason => journal.Add($"hidden {reason}"));
            var runner = new SequenceRunner("instant", new ISequenceStep[] { step });

            runner.Start();

            Assert.AreEqual(new[] { "shown", "hidden Completed" }, journal);
        }

        [Test]
        public void A_parallel_step_finishes_only_when_every_child_has_finished()
        {
            var journal = new List<string>();
            var left = new RecordingStep("left", journal);
            var right = new RecordingStep("right", journal);
            var runner = Sequence.Named("both").Together("both hands", left, right).Build();
            runner.Start();

            left.FinishNow();
            Assert.AreEqual(SequenceState.Running, runner.State);

            right.FinishNow();
            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void A_parallel_step_forwards_time_only_to_unfinished_children()
        {
            var journal = new List<string>();
            var left = new RecordingStep("left", journal);
            var right = new RecordingStep("right", journal);
            var runner = Sequence.Named("both").Together("both hands", left, right).Build();
            runner.Start();
            left.FinishNow();

            runner.Advance(1f);

            Assert.AreEqual(0f, left.AdvancedSeconds);
            Assert.AreEqual(1f, right.AdvancedSeconds, 0.0001f);
        }

        [Test]
        public void Cancelling_a_parallel_step_cancels_the_children_that_had_not_finished()
        {
            var journal = new List<string>();
            var left = new RecordingStep("left", journal);
            var right = new RecordingStep("right", journal);
            var runner = Sequence.Named("both").Together("both hands", left, right).Build();
            runner.Start();
            left.FinishNow();

            runner.Cancel();

            Assert.AreEqual(ExitReason.Completed, left.LastExitReason);
            Assert.AreEqual(ExitReason.Cancelled, right.LastExitReason);
        }

        [Test]
        public void A_reminding_step_reminds_each_interval_until_the_inner_step_finishes()
        {
            var journal = new List<string>();
            var inner = new RecordingStep("grab the paddle", journal);
            var reminders = 0;
            var reminding = new RemindingStep(inner, 2f, () => reminders++);
            var runner = new SequenceRunner("remind", new ISequenceStep[] { reminding });
            runner.Start();

            runner.Advance(1f);
            runner.Advance(1.5f);
            runner.Advance(2f);
            Assert.AreEqual(2, reminders);

            inner.FinishNow();
            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void A_reminding_step_keeps_the_name_of_the_step_it_wraps()
        {
            var inner = new RecordingStep("grab the paddle", new List<string>());

            Assert.AreEqual("grab the paddle", new RemindingStep(inner, 1f, () => { }).Name);
        }

        [Test]
        public void The_builder_keeps_the_order_the_steps_were_told_in()
        {
            var journal = new List<string>();
            var runner = Sequence.Named("story")
                .Do("open the door", () => journal.Add("door"))
                .Wait(0f)
                .Do("walk in", () => journal.Add("walk"))
                .Build();

            runner.Start();

            Assert.AreEqual(new[] { "door", "walk" }, journal);
            Assert.AreEqual("story", runner.Name);
            Assert.AreEqual(3, runner.StepCount);
        }
    }
}
