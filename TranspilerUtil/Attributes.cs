using JetBrains.Annotations;
using System;

namespace TranspilerUtil
{
    public abstract class InfixTargetAttribute(Patcher.PatchType patchType, Type type, string memberName, Type[] parameterTypes) : Attribute
    {
        public readonly Patcher.PatchType patchType = patchType;
        public readonly Type type = type;
        public readonly string memberName = memberName;
        public readonly Type[] parameterTypes = parameterTypes;
    }


    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class InfixWrapperAttribute(Type type, string memberName, Type[] parameterTypes = null) : InfixTargetAttribute(Patcher.PatchType.Wrapper, type, memberName, parameterTypes)
    {
    }

    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class InfixPrefixAttribute(Type type, string memberName, Type[] parameterTypes = null) : InfixTargetAttribute(Patcher.PatchType.Prefix, type, memberName, parameterTypes)
    {
    }

    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class InfixPostfixAttribute(Type type, string memberName, Type[] parameterTypes = null) : InfixTargetAttribute(Patcher.PatchType.Postfix, type, memberName, parameterTypes)
    {
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

