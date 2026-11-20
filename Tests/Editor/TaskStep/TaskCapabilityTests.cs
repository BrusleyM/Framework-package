using System.Linq;
using System.Reflection;
using Framework.Tasks;
using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class TaskCapabilityTests
    {
        [Test]
        public void The_base_task_only_has_the_core_lifecycle_as_overridable_members()
        {
            var overridable = typeof(Task)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(method => method.IsVirtual && !method.IsFinal)
                .Select(method => method.Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.That(overridable, Is.EqualTo(new[] { "CancelTask", "Prepare", "Skip", "StartTask", "TaskComplete" }));
        }

        [Test]
        public void An_action_task_has_every_capability()
        {
            var capabilities = typeof(ActionTask).GetInterfaces();

            Assert.That(capabilities, Does.Contain(typeof(IUnguidedTask)));
            Assert.That(capabilities, Does.Contain(typeof(IHintable)));
            Assert.That(capabilities, Does.Contain(typeof(IPausableTask)));
            Assert.That(capabilities, Does.Contain(typeof(ISettlingTask)));
        }

        [Test]
        public void An_audio_task_can_pause_and_only_gives_guidance_but_cannot_hint()
        {
            var capabilities = typeof(AudioTask).GetInterfaces();

            Assert.That(capabilities, Does.Contain(typeof(IPausableTask)));
            Assert.That(capabilities, Does.Contain(typeof(IGuidanceOnlyTask)));
            Assert.That(capabilities.Contains(typeof(IHintable)), Is.False);
        }
    }
}
