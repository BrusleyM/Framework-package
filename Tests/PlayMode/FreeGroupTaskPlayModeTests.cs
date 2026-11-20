using System.Collections;
using System.Reflection;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class FreeGroupTaskPlayModeTests
    {
        private GameObject _host;
        private SequentialTask _sequence;
        private FreeGroupTask _group;
        private HintProbeTask _first;
        private HintProbeTask _second;
        private ProbeTask _after;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("sequence host");
            _sequence = _host.AddComponent<SequentialTask>();
            _group = _host.AddComponent<FreeGroupTask>();
            _first = _host.AddComponent<HintProbeTask>();
            _second = _host.AddComponent<HintProbeTask>();
            _after = _host.AddComponent<ProbeTask>();
            SetPrivate(_group, "_tasks", new System.Collections.Generic.List<Task> { _first, _second });
            _sequence.AddTask(_group);
            _sequence.AddTask(_after);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
        }

        [UnityTest]
        public IEnumerator A_free_group_in_a_sequence_holds_the_next_task_until_every_group_task_is_done_in_any_order()
        {
            _sequence.Run();
            yield return null;

            _second.Finish();
            yield return null;
            var startedTooEarly = _after.StartCount;
            _first.Finish();
            yield return null;

            Assert.That(startedTooEarly, Is.EqualTo(0));
            Assert.That(_after.StartCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Nothing_is_hinted_until_it_is_asked_for()
        {
            _sequence.Run();
            yield return new WaitForSeconds(0.3f);

            Assert.That(_first.HintShowCount, Is.EqualTo(0));
            Assert.That(_second.HintShowCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Asking_for_a_hint_while_the_sequence_runs_hints_the_first_open_task()
        {
            _sequence.Run();
            yield return null;

            _group.RequestHint();

            Assert.That(_first.HintShowCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator With_an_idle_time_a_hint_appears_by_itself_after_that_long_without_progress()
        {
            SetPrivate(_group, "_hintAfterIdleSeconds", 0.3f);
            _sequence.Run();
            yield return new WaitForSeconds(0.1f);
            var hintedEarly = _first.HintShowCount;

            yield return new WaitForSeconds(0.5f);

            Assert.That(hintedEarly, Is.EqualTo(0));
            Assert.That(_first.HintShowCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Progress_restarts_the_idle_time_so_the_hint_does_not_come_while_the_user_is_busy()
        {
            SetPrivate(_group, "_hintAfterIdleSeconds", 0.5f);
            _sequence.Run();
            yield return new WaitForSeconds(0.3f);
            _first.Finish();
            yield return new WaitForSeconds(0.3f);

            var hintedWhileBusy = _second.HintShowCount;
            yield return new WaitForSeconds(0.5f);

            Assert.That(hintedWhileBusy, Is.EqualTo(0));
            Assert.That(_second.HintShowCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator The_idle_hint_is_not_repeated_for_a_task_that_is_already_hinted()
        {
            SetPrivate(_group, "_hintAfterIdleSeconds", 0.2f);
            _sequence.Run();

            yield return new WaitForSeconds(0.9f);

            Assert.That(_first.HintShowCount, Is.EqualTo(1));
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class HintProbeTask : Task
        {
            public int HintShowCount { get; private set; }

            public void Finish() => TaskComplete();

            public override void StartTask()
            {
            }

            public override void ShowHint() => HintShowCount++;
        }
    }
}
