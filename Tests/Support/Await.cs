using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public static class Await
    {
        public const float DefaultTimeoutSeconds = 10f;

        public static IEnumerator Until(Func<bool> condition, string description, float timeoutSeconds = DefaultTimeoutSeconds)
        {
            var started = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - started > timeoutSeconds)
                {
                    Assert.Fail($"Timed out after {timeoutSeconds} seconds waiting until {description}.");
                }

                yield return null;
            }
        }
    }
}
