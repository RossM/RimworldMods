using System;
using JetBrains.Annotations;

// ReSharper disable UnusedParameter.Local

namespace XylXenos
{
    // This attribute serves as documentation of which patches are to support which parts of the mod. It
    // has no actual effect.

    public class FeatureAttribute : Attribute
    {
#pragma warning disable CS9113 // Parameter is unread.
        public FeatureAttribute(params string[] featureNames)
        {
        }

        public FeatureAttribute(Config.Feature feature)
        {
        }

        public FeatureAttribute(Type feature)
        {
        }
#pragma warning restore CS9113 // Parameter is unread.
    }

    [MeansImplicitUse]
    public class UsedFromXmlAttribute : Attribute;

    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class WrappedMemberAttribute(Type type, string memberName, Type[] parameterTypes = null) : Attribute
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
