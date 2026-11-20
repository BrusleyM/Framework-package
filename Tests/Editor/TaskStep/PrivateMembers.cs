using System.Reflection;

namespace Framework.Sequencing.Tests
{
    internal static class PrivateMembers
    {
        private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static void SetField(object target, string fieldName, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(fieldName, Declared);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }

            throw new System.MissingFieldException(target.GetType().Name, fieldName);
        }

        public static void Invoke(object target, string methodName)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var method = type.GetMethod(methodName, Declared);
                if (method != null)
                {
                    method.Invoke(target, null);
                    return;
                }
            }

            throw new System.MissingMethodException(target.GetType().Name, methodName);
        }
    }
}
