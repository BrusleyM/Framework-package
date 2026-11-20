using System.Collections.Generic;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class SubtaskSequencerTests
    {
        private GameObject _host;
        private ScriptedSubtask _first;
        private ScriptedSubtask _second;
        private ScriptedSubtask _setup;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
            _first = _host.AddComponent<ScriptedSubtask>();
            _second = _host.AddComponent<ScriptedSubtask>();
            _setup = _host.AddComponent<ScriptedSubtask>();
            PrivateMembers.SetField(_setup, "_isSetup", true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void Subtasks_run_one_after_another_each_starting_when_the_previous_completes()
        {
            var sequencer = new SubtaskSequencer(new GameObjectTask[] { _first, _second });

            sequencer.Start(SubtaskFilter.All);
            var secondStartedEarly = _second.ExecuteCount;
            _first.Finish();

            Assert.That(_first.ExecuteCount, Is.EqualTo(1));
            Assert.That(secondStartedEarly, Is.EqualTo(0));
            Assert.That(_second.ExecuteCount, Is.EqualTo(1));
            Assert.That(sequencer.Current, Is.SameAs(_second));
        }

        [Test]
        public void After_the_last_subtask_completes_there_is_no_current_subtask()
        {
            var sequencer = new SubtaskSequencer(new GameObjectTask[] { _first });
            sequencer.Start(SubtaskFilter.All);

            _first.Finish();

            Assert.That(sequencer.Current, Is.Null);
        }

        [Test]
        public void Subtasks_that_complete_as_they_start_do_not_nest_and_all_run()
        {
            _first.CompleteWhenExecuted = true;
            _second.CompleteWhenExecuted = true;
            var sequencer = new SubtaskSequencer(new GameObjectTask[] { _first, _second });

            sequencer.Start(SubtaskFilter.All);

            Assert.That(_first.ExecuteCount, Is.EqualTo(1));
            Assert.That(_second.ExecuteCount, Is.EqualTo(1));
            Assert.That(sequencer.Current, Is.Null);
        }

        [Test]
        public void The_guidance_filter_skips_the_setup_subtasks()
        {
            var sequencer = new SubtaskSequencer(new GameObjectTask[] { _setup, _first });

            sequencer.Start(SubtaskFilter.GuidanceOnly);

            Assert.That(_setup.ExecuteCount, Is.EqualTo(0));
            Assert.That(_first.ExecuteCount, Is.EqualTo(1));
        }

        [Test]
        public void Running_the_setup_runs_only_the_setup_subtasks_without_chaining()
        {
            var sequencer = new SubtaskSequencer(new GameObjectTask[] { _first, _setup });

            sequencer.RunSetup();

            Assert.That(_first.ExecuteCount, Is.EqualTo(0));
            Assert.That(_setup.ExecuteCount, Is.EqualTo(1));
        }

        [Test]
        public void Stopping_detaches_so_a_later_completion_does_not_start_the_next_subtask()
        {
            var sequencer = new SubtaskSequencer(new GameObjectTask[] { _first, _second });
            sequencer.Start(SubtaskFilter.All);

            sequencer.Stop();
            _first.Finish();

            Assert.That(_second.ExecuteCount, Is.EqualTo(0));
        }

        [Test]
        public void Tearing_down_cancels_every_subtask_but_with_the_guidance_filter_keeps_the_setup()
        {
            var sequencer = new SubtaskSequencer(new GameObjectTask[] { _first, _setup });

            sequencer.TearDown(SubtaskFilter.GuidanceOnly);
            var cancelledSetup = _setup.CancelCount;
            sequencer.TearDown(SubtaskFilter.All);

            Assert.That(_first.CancelCount, Is.EqualTo(2));
            Assert.That(cancelledSetup, Is.EqualTo(0));
            Assert.That(_setup.CancelCount, Is.EqualTo(1));
        }

        [Test]
        public void Missing_and_empty_subtask_lists_are_fine()
        {
            var empty = new SubtaskSequencer(null);
            var withGap = new SubtaskSequencer(new GameObjectTask[] { null, _first });

            Assert.DoesNotThrow(() => empty.Start(SubtaskFilter.All));
            Assert.DoesNotThrow(() => empty.TearDown(SubtaskFilter.All));
            withGap.Start(SubtaskFilter.All);
            Assert.That(_first.ExecuteCount, Is.EqualTo(1));
        }

        private sealed class ScriptedSubtask : GameObjectTask
        {
            public int ExecuteCount { get; private set; }
            public int CancelCount { get; private set; }
            public bool CompleteWhenExecuted { get; set; }

            public void Finish() => MarkComplete();

            public override void Execute()
            {
                ExecuteCount++;
                if (CompleteWhenExecuted)
                {
                    MarkComplete();
                }
            }

            public override void Cancel() => CancelCount++;
        }
    }
}
