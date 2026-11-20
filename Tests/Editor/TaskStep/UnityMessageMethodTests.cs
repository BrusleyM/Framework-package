using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class UnityMessageMethodTests
    {
        private static readonly string[] ParameterlessMessages =
        {
            "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable",
            "OnDestroy", "OnValidate", "Reset", "OnGUI", "OnApplicationQuit"
        };

        [Test]
        public void No_framework_component_declares_a_unity_message_name_with_parameters()
        {
            var offenders = new List<string>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>().Where(IsFrameworkType))
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                offenders.AddRange(methods
                    .Where(method => ParameterlessMessages.Contains(method.Name) && method.GetParameters().Length > 0)
                    .Select(method => type.FullName + "." + method.Name));
            }

            Assert.That(offenders, Is.Empty, "Unity reserves these names and logs an error when they take parameters.");
        }

        private static bool IsFrameworkType(System.Type type)
        {
            return type.Namespace != null && type.Namespace.StartsWith("Framework");
        }
    }
}
