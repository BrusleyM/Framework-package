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
        public void The_base_task_implements_no_interfaces_itself()
        {
            Assert.That(typeof(Task).GetInterfaces(), Is.Empty);
        }

        [Test]
        public void The_capability_methods_of_the_guided_and_audio_tasks_can_be_overridden_by_a_subclass()
        {
            var overridable = new[]
            {
                typeof(GuidedTask).GetMethod("ShowHint"), typeof(GuidedTask).GetMethod("HideHint"),
                typeof(GuidedTask).GetMethod("StartTaskUnguided"), typeof(GuidedTask).GetMethod("PauseTask"),
                typeof(GuidedTask).GetMethod("ResumeTask"), typeof(GuidedTask).GetProperty("IsSettling").GetGetMethod(),
                typeof(AudioTask).GetMethod("PauseTask"), typeof(AudioTask).GetMethod("ResumeTask")
            };

            Assert.That(overridable.All(method => method.IsVirtual), Is.True);
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
