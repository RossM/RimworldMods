using JetBrains.Annotations;
using System;

namespace TranspilerUtil;

public abstract class InfixTargetAttribute(InfixPatcher.PatchType patchType, Type type, string memberName, Type[] parameterTypes) : Attribute
{
    public readonly InfixPatcher.PatchType patchType = patchType;
    public readonly Type type = type;
    public readonly string memberName = memberName;
    public readonly Type[] parameterTypes = parameterTypes;
}


[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InfixPrefixAttribute(Type type, string memberName, Type[] parameterTypes = null) : InfixTargetAttribute(InfixPatcher.PatchType.Prefix, type, memberName, parameterTypes);

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InfixPostfixAttribute(Type type, string memberName, Type[] parameterTypes = null) : InfixTargetAttribute(InfixPatcher.PatchType.Postfix, type, memberName, parameterTypes);

/// <summary>
/// This attribute causes the infix transpiler to log the instruction sequence of the modified method to the debug log.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class InfixDebugAttribute : Attribute;

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