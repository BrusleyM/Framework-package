using NUnit.Framework;
using UnityEngine;

namespace Framework.Samples.Tests
{
    public class FollowRulesTests
    {
        private static readonly Vector3 Leader = new Vector3(0f, 1.7f, 0f);

        [Test]
        public void A_follower_inside_the_comfort_band_is_not_outside_it()
        {
            Assert.That(FollowRules.IsOutsideComfortBand(new Vector3(6f, 3f, 0f), Leader, 5f, 10f), Is.False);
        }

        [Test]
        public void A_follower_closer_than_the_minimum_is_outside_the_band()
        {
            Assert.That(FollowRules.IsOutsideComfortBand(new Vector3(2f, 3f, 0f), Leader, 5f, 10f), Is.True);
        }

        [Test]
        public void A_follower_farther_than_the_maximum_is_outside_the_band()
        {
            Assert.That(FollowRules.IsOutsideComfortBand(new Vector3(12f, 3f, 0f), Leader, 5f, 10f), Is.True);
        }

        [Test]
        public void Height_differences_do_not_count_towards_the_distance()
        {
            Assert.That(FollowRules.IsOutsideComfortBand(new Vector3(6f, 40f, 0f), Leader, 5f, 10f), Is.False);
        }

        [Test]
        public void A_small_turn_of_the_leader_stays_under_the_threshold()
        {
            var follower = new Vector3(6f, 0f, 0f);
            var desired = Quaternion.Euler(0f, 20f, 0f) * follower;

            Assert.That(FollowRules.HasLeaderTurnedAway(follower, desired, Leader, 40f), Is.False);
        }

        [Test]
        public void A_big_turn_of_the_leader_passes_the_threshold()
        {
            var follower = new Vector3(6f, 0f, 0f);
            var desired = Quaternion.Euler(0f, 90f, 0f) * follower;

            Assert.That(FollowRules.HasLeaderTurnedAway(follower, desired, Leader, 40f), Is.True);
        }

        [Test]
        public void A_follower_standing_on_the_leader_has_no_turn_to_measure()
        {
            Assert.That(FollowRules.HasLeaderTurnedAway(Leader, new Vector3(5f, 0f, 0f), Leader, 40f), Is.False);
        }

        [Test]
        public void A_position_inside_the_zone_is_left_alone()
        {
            var position = new Vector3(3f, 1.5f, 2f);

            Assert.That(FollowRules.ClampToZone(position, Vector3.zero, 8f), Is.EqualTo(position));
        }

        [Test]
        public void A_position_outside_the_zone_is_pulled_back_to_its_edge_keeping_the_height()
        {
            var clamped = FollowRules.ClampToZone(new Vector3(20f, 1.5f, 0f), Vector3.zero, 8f);

            Assert.That(clamped.x, Is.EqualTo(8f).Within(0.001f));
            Assert.That(clamped.y, Is.EqualTo(1.5f));
        }

        [Test]
        public void The_zone_is_measured_along_the_floor_around_its_centre()
        {
            var centre = new Vector3(10f, 0f, 10f);

            Assert.That(FollowRules.IsInsideZone(new Vector3(12f, 50f, 10f), centre, 3f), Is.True);
            Assert.That(FollowRules.IsInsideZone(new Vector3(20f, 0f, 10f), centre, 3f), Is.False);
        }
    }
}
