using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class CompletionJoinTests
    {
        private CompletionJoin<string> _join;
        private int _raised;

        [SetUp]
        public void SetUp()
        {
            _join = new CompletionJoin<string>();
            _raised = 0;
            _join.AllFinished += () => _raised++;
        }

        [Test]
        public void The_join_is_raised_once_when_the_last_member_finishes()
        {
            _join.Begin(new[] { "a", "b" });
            _join.Release();

            _join.Finish("a");
            Assert.That(_raised, Is.EqualTo(0));

            _join.Finish("b");
            Assert.That(_raised, Is.EqualTo(1));
        }

        [Test]
        public void Members_that_finish_while_the_join_is_held_raise_it_only_on_release()
        {
            _join.Begin(new[] { "a", "b" });

            _join.Finish("a");
            _join.Finish("b");
            Assert.That(_raised, Is.EqualTo(0));

            _join.Release();
            Assert.That(_raised, Is.EqualTo(1));
        }

        [Test]
        public void Every_hold_needs_its_own_release_before_the_join_is_raised()
        {
            _join.Begin(new[] { "a" });
            _join.Release();
            _join.Hold();
            _join.Hold();

            _join.Finish("a");
            _join.Release();
            Assert.That(_raised, Is.EqualTo(0));

            _join.Release();
            Assert.That(_raised, Is.EqualTo(1));
        }

        [Test]
        public void A_held_join_reports_that_every_member_finished_before_it_is_raised()
        {
            _join.Begin(new[] { "a", "b" });

            _join.Finish("a");
            var afterFirst = _join.HasEveryMemberFinished;
            _join.Finish("b");

            Assert.That(afterFirst, Is.False);
            Assert.That(_join.HasEveryMemberFinished, Is.True);
            Assert.That(_raised, Is.EqualTo(0));
        }

        [Test]
        public void A_join_without_members_is_raised_on_release()
        {
            _join.Begin(new string[0]);

            _join.Release();

            Assert.That(_raised, Is.EqualTo(1));
        }

        [Test]
        public void Finishing_the_same_member_twice_counts_once_and_reports_it_was_not_new()
        {
            _join.Begin(new[] { "a", "b" });
            _join.Release();

            var first = _join.Finish("a");
            var second = _join.Finish("a");

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(_join.FinishedCount, Is.EqualTo(1));
            Assert.That(_raised, Is.EqualTo(0));
        }

        [Test]
        public void A_member_listed_twice_is_one_member()
        {
            _join.Begin(new[] { "a", "a" });
            _join.Release();

            _join.Finish("a");

            Assert.That(_join.MemberCount, Is.EqualTo(1));
            Assert.That(_raised, Is.EqualTo(1));
        }

        [Test]
        public void A_stranger_cannot_finish_the_join()
        {
            _join.Begin(new[] { "a" });
            _join.Release();

            var accepted = _join.Finish("stranger");

            Assert.That(accepted, Is.False);
            Assert.That(_raised, Is.EqualTo(0));
        }

        [Test]
        public void A_stopped_join_ignores_finishes_and_never_raises()
        {
            _join.Begin(new[] { "a" });
            _join.Release();

            _join.Stop();
            var accepted = _join.Finish("a");

            Assert.That(accepted, Is.False);
            Assert.That(_raised, Is.EqualTo(0));
            Assert.That(_join.IsActive, Is.False);
        }

        [Test]
        public void A_raised_join_is_not_raised_again_by_late_finishes_or_a_second_release()
        {
            _join.Begin(new[] { "a" });
            _join.Release();
            _join.Finish("a");

            _join.Finish("a");
            _join.Release();

            Assert.That(_raised, Is.EqualTo(1));
        }

        [Test]
        public void Beginning_again_starts_over_with_nothing_finished()
        {
            _join.Begin(new[] { "a" });
            _join.Release();
            _join.Finish("a");

            _join.Begin(new[] { "a", "b" });

            Assert.That(_join.FinishedCount, Is.EqualTo(0));
            Assert.That(_join.IsFinished("a"), Is.False);
            Assert.That(_join.IsActive, Is.True);
        }

        [Test]
        public void A_member_finishing_inside_the_raised_handler_does_not_raise_twice()
        {
            _join.Begin(new[] { "a", "b" });
            _join.Release();
            var activeInsideHandler = true;
            var acceptedInsideHandler = true;
            _join.AllFinished += () =>
            {
                activeInsideHandler = _join.IsActive;
                acceptedInsideHandler = _join.Finish("b");
                _join.Release();
            };

            _join.Finish("a");
            _join.Finish("b");

            Assert.That(activeInsideHandler, Is.False);
            Assert.That(acceptedInsideHandler, Is.False);
            Assert.That(_raised, Is.EqualTo(1));
        }

        [Test]
        public void Holding_or_releasing_a_stopped_join_raises_nothing()
        {
            _join.Begin(new[] { "a" });
            _join.Release();
            _join.Finish("a");
            _join.Stop();

            _join.Hold();
            _join.Release();
            _join.Release();

            Assert.That(_raised, Is.EqualTo(1));
        }

        [Test]
        public void Releasing_more_often_than_holding_never_raises_early()
        {
            _join.Begin(new[] { "a", "b" });
            _join.Release();
            _join.Release();
            _join.Release();

            _join.Finish("a");

            Assert.That(_raised, Is.EqualTo(0));
        }
    }
}
