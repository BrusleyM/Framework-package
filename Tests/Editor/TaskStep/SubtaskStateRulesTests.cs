using Framework.Tasks;
using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class SubtaskStateRulesTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void On_always_resolves_to_on_whatever_the_current_state_is(bool isCurrentlyOn)
        {
            Assert.IsTrue(SubtaskStateRules.ResolvesToOn(SubtaskState.On, isCurrentlyOn));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Off_always_resolves_to_off_whatever_the_current_state_is(bool isCurrentlyOn)
        {
            Assert.IsFalse(SubtaskStateRules.ResolvesToOn(SubtaskState.Off, isCurrentlyOn));
        }

        [Test]
        public void Toggle_resolves_to_on_when_it_is_currently_off()
        {
            Assert.IsTrue(SubtaskStateRules.ResolvesToOn(SubtaskState.Toggle, false));
        }

        [Test]
        public void Toggle_resolves_to_off_when_it_is_currently_on()
        {
            Assert.IsFalse(SubtaskStateRules.ResolvesToOn(SubtaskState.Toggle, true));
        }
    }
}
