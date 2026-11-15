using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class StageTests
    {
        private List<string> _journal;
        private RecordingStep _introHello;
        private RecordingStep _introGrab;
        private RecordingStep _handsPoint;
        private RecordingStep _handsPinch;
        private StageStep _intro;
        private StageStep _hands;
        private SequenceRunner _tutorial;

        [SetUp]
        public void SetUp()
        {
            _journal = new List<string>();
            _introHello = new RecordingStep("hello", _journal);
            _introGrab = new RecordingStep("grab", _journal);
            _handsPoint = new RecordingStep("point", _journal);
            _handsPinch = new RecordingStep("pinch", _journal);
            _intro = new StageStep("intro", new SequenceRunner("intro", new ISequenceStep[] { _introHello, _introGrab }));
            _hands = new StageStep("hands", new SequenceRunner("hands", new ISequenceStep[] { _handsPoint, _handsPinch }));
            _tutorial = new SequenceRunner("tutorial", new ISequenceStep[] { _intro, _hands });
        }

        [Test]
        public void A_stage_runs_its_own_steps_in_order_and_then_hands_over_to_the_next_stage()
        {
            _tutorial.Start();

            _introHello.FinishNow();
            _introGrab.FinishNow();

            Assert.AreEqual(1, _tutorial.CurrentIndex);
            Assert.AreSame(_hands, _tutorial.CurrentStep);
            Assert.AreSame(_handsPoint, _hands.Stage.CurrentStep);
        }

        [Test]
        public void Finishing_the_last_step_of_the_last_stage_completes_the_whole_sequence()
        {
            _tutorial.Start();
            _introHello.FinishNow();
            _introGrab.FinishNow();
            _handsPoint.FinishNow();
            _handsPinch.FinishNow();

            Assert.AreEqual(SequenceState.Completed, _tutorial.State);
        }

        [Test]
        public void Skipping_a_stage_cleans_up_its_running_step_and_starts_the_next_stage()
        {
            _tutorial.Start();

            _tutorial.SkipCurrentStep();

            Assert.AreEqual(ExitReason.Cancelled, _introHello.LastExitReason);
            Assert.AreEqual(0, _introGrab.AdvancedSeconds);
            Assert.AreSame(_handsPoint, _hands.Stage.CurrentStep);
        }

        [Test]
        public void Restarting_the_current_stage_begins_it_again_from_its_first_step()
        {
            _tutorial.Start();
            _introHello.FinishNow();
            Assert.AreSame(_introGrab, _intro.Stage.CurrentStep);

            _tutorial.RestartCurrentStep();

            Assert.AreEqual(0, _tutorial.CurrentIndex);
            Assert.AreSame(_introHello, _intro.Stage.CurrentStep);
            Assert.AreEqual(ExitReason.Cancelled, _introGrab.LastExitReason);
        }

        [Test]
        public void Restarting_a_later_stage_leaves_the_earlier_stage_untouched()
        {
            _tutorial.Start();
            _tutorial.SkipCurrentStep();
            _handsPoint.FinishNow();

            _tutorial.RestartCurrentStep();

            Assert.AreEqual(1, _tutorial.CurrentIndex);
            Assert.AreSame(_handsPoint, _hands.Stage.CurrentStep);
            Assert.AreEqual(1, _journal.FindAll(entry => entry == "enter hello").Count);
        }

        [Test]
        public void Going_back_from_a_stage_returns_to_the_previous_stage_from_its_first_step()
        {
            _tutorial.Start();
            _introHello.FinishNow();
            _introGrab.FinishNow();

            _tutorial.GoBack();

            Assert.AreEqual(0, _tutorial.CurrentIndex);
            Assert.AreSame(_introHello, _intro.Stage.CurrentStep);
        }

        [Test]
        public void Time_reaches_the_step_running_inside_the_stage()
        {
            _tutorial.Start();

            _tutorial.Advance(0.5f);

            Assert.AreEqual(0.5f, _introHello.AdvancedSeconds, 0.0001f);
        }

        [Test]
        public void Pausing_the_sequence_pauses_the_stage_so_its_steps_wait_for_resume()
        {
            _tutorial.Start();
            _tutorial.Pause();

            _introHello.FinishNow();
            Assert.AreSame(_introHello, _intro.Stage.CurrentStep);
            Assert.AreEqual(1, _introHello.PauseCount);

            _tutorial.Resume();
            Assert.AreSame(_introGrab, _intro.Stage.CurrentStep);
        }

        [Test]
        public void A_stage_entered_while_the_sequence_is_paused_starts_paused()
        {
            _tutorial.Start();
            _tutorial.Pause();

            _tutorial.SkipCurrentStep();

            Assert.AreEqual(1, _handsPoint.PauseCount);
        }

        [Test]
        public void Cancelling_the_sequence_cleans_up_the_step_running_inside_the_stage()
        {
            _tutorial.Start();

            _tutorial.Cancel();

            Assert.AreEqual(ExitReason.Cancelled, _introHello.LastExitReason);
            Assert.AreEqual(SequenceState.Cancelled, _tutorial.State);
        }

        [Test]
        public void A_step_failing_inside_a_stage_faults_the_whole_sequence_with_that_exception()
        {
            Exception reported = null;
            _tutorial.Faulted += exception => reported = exception;
            _tutorial.Start();
            var failure = new InvalidOperationException("pose lost");

            _introHello.FailNow(failure);

            Assert.AreEqual(SequenceState.Faulted, _tutorial.State);
            Assert.AreSame(failure, reported);
        }

        [Test]
        public void The_builder_describes_a_stage_as_a_named_group_of_steps()
        {
            var entered = new List<string>();
            var tutorial = Sequence.Named("tutorial")
                .Stage("intro", stage => stage.Do("hello", () => entered.Add("hello")).Do("grab", () => entered.Add("grab")))
                .Stage("hands", stage => stage.Do("point", () => entered.Add("point")))
                .Build();

            tutorial.Start();

            Assert.AreEqual(new[] { "hello", "grab", "point" }, entered);
            Assert.AreEqual(SequenceState.Completed, tutorial.State);
            Assert.AreEqual(2, tutorial.StepCount);
        }
    }
}
