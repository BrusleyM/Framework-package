using Framework.Sequencing.Editor;
using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class CoreVersionCheckTests
    {
        private const string SequencerNeedingCore120 = "{\"version\":\"3.2.0\",\"requiresCore\":\"1.2.0\"}";

        [Test]
        public void A_core_at_the_required_version_has_no_problem()
        {
            Assert.That(CoreVersionCheck.FindProblem(SequencerNeedingCore120, "{\"version\":\"1.2.0\"}"), Is.Null);
        }

        [Test]
        public void A_core_older_than_the_required_version_is_reported_with_both_versions()
        {
            var problem = CoreVersionCheck.FindProblem(SequencerNeedingCore120, "{\"version\":\"1.1.0\"}");

            Assert.That(problem, Does.Contain("1.2.0").And.Contain("1.1.0"));
        }

        [Test]
        public void A_sequencer_manifest_without_a_required_core_is_reported_as_a_manifest_mistake()
        {
            Assert.That(CoreVersionCheck.FindProblem("{\"version\":\"3.2.0\"}", "{\"version\":\"1.2.0\"}"), Does.Contain("not a version"));
        }
    }
}
