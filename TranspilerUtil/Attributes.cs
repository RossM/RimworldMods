using System;
using JetBrains.Annotations;

namespace TranspilerUtil;

public abstract class TranspilerTargetAttribute(
    Autopatcher.PatchType patchType,
    Type type,
    string memberName,
    Type[]? parameterTypes,
    Type[]? genericTypes) : Attribute
{
    public readonly Autopatcher.PatchType patchType = patchType;
    public readonly Type type = type;
    public readonly string memberName = memberName;
    public readonly Type[]? parameterTypes = parameterTypes;
    public readonly Type[]? genericTypes = genericTypes;
}

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InnerPrefixAttribute(Type type, string memberName, Type[]? parameterTypes = null, Type[]? genericTypes = null)
    : TranspilerTargetAttribute(Autopatcher.PatchType.InnerPrefix, type, memberName, parameterTypes, genericTypes);

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class InnerPostfixAttribute(Type type, string memberName, Type[]? parameterTypes = null, Type[]? genericTypes = null)
    : TranspilerTargetAttribute(Autopatcher.PatchType.InnerPostfix, type, memberName, parameterTypes, genericTypes);

/// <summary>
///     This attribute causes the infix transpiler to log the instruction sequence of the modified method to the debug log.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DebugAttribute : Attribute;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TargetAttribute(Type? type, string methodName, Type[]? parameterTypes = null, Type[]? genericTypes = null) : Attribute
{
    public readonly Type? type = type;
    public readonly string methodName = methodName;
    public readonly Type[]? parameterTypes = parameterTypes;
    public readonly Type[]? genericTypes = genericTypes;

    public TargetAttribute(string methodName, Type[]? parameterTypes = null, Type[]? genericTypes = null) : this(null, methodName,
        parameterTypes, genericTypes)
    {
    }
}
