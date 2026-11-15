using System.Collections.Generic;
using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class StepWindowTests
    {
        private List<string> _journal;
        private SequenceRunner _runner;

        [SetUp]
        public void SetUp()
        {
            _journal = new List<string>();
            _runner = new SequenceRunner("Window", new ISequenceStep[]
            {
                new RecordingStep("first", _journal),
                new RecordingStep("second", _journal),
                new RecordingStep("third", _journal)
            });
        }

        [Test]
        public void Before_the_sequence_starts_there_is_no_current_step_and_the_first_step_is_next()
        {
            var window = StepWindow.Of(_runner);

            Assert.IsNull(window.PreviousName);
            Assert.IsNull(window.CurrentName);
            Assert.AreEqual("first", window.NextName);
            Assert.AreEqual(0, window.Position);
            Assert.AreEqual(3, window.Count);
        }

        [Test]
        public void On_the_first_step_there_is_no_previous_step_and_the_second_is_next()
        {
            _runner.Start();

            var window = StepWindow.Of(_runner);

            Assert.IsNull(window.PreviousName);
            Assert.AreEqual("first", window.CurrentName);
            Assert.AreEqual("second", window.NextName);
            Assert.AreEqual(1, window.Position);
        }

        [Test]
        public void On_a_middle_step_both_neighbours_are_named()
        {
            _runner.Start();
            _runner.SkipCurrentStep();

            var window = StepWindow.Of(_runner);

            Assert.AreEqual("first", window.PreviousName);
            Assert.AreEqual("second", window.CurrentName);
            Assert.AreEqual("third", window.NextName);
            Assert.AreEqual(2, window.Position);
        }

        [Test]
        public void On_the_last_step_there_is_no_next_step()
        {
            _runner.Start();
            _runner.SkipCurrentStep();
            _runner.SkipCurrentStep();

            var window = StepWindow.Of(_runner);

            Assert.AreEqual("second", window.PreviousName);
            Assert.AreEqual("third", window.CurrentName);
            Assert.IsNull(window.NextName);
        }

        [Test]
        public void After_the_sequence_completes_the_last_step_is_previous_and_nothing_is_current_or_next()
        {
            _runner.Start();
            _runner.SkipCurrentStep();
            _runner.SkipCurrentStep();
            _runner.SkipCurrentStep();

            var window = StepWindow.Of(_runner);

            Assert.AreEqual("third", window.PreviousName);
            Assert.IsNull(window.CurrentName);
            Assert.IsNull(window.NextName);
            Assert.AreEqual(0, window.Position);
        }

        [Test]
        public void An_empty_sequence_has_an_empty_window()
        {
            var window = StepWindow.Of(new SequenceRunner("Empty", new ISequenceStep[0]));

            Assert.IsNull(window.PreviousName);
            Assert.IsNull(window.CurrentName);
            Assert.IsNull(window.NextName);
            Assert.AreEqual(0, window.Count);
        }
    }
}
