using JetBrains.Annotations;
using System;

namespace TranspilerUtil
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class InfixWrapperAttribute(Type type, string memberName, Type[] parameterTypes = null) : Attribute
    {
        public readonly Type type = type;
        public readonly string memberName = memberName;
        public readonly Type[] parameterTypes = parameterTypes;
    }

    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class InfixPatchAttribute(Type type, string methodName, Type[] parameterTypes = null) : Attribute
    {
        public readonly Type type = type;
        public readonly string methodName = methodName;
        public readonly Type[] parameterTypes = parameterTypes;

        public InfixPatchAttribute(string methodName, Type[] parameterTypes = null) : this(null, methodName, parameterTypes)
        {
        }
    }
}

