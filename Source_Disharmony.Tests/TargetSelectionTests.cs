using NUnit.Framework;
using System;
using System.Reflection;

namespace Disharmony.Tests;

public static class TargetSelectionPatchMethods
{
    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.MutableProperty), MemberType.Setter)]
    public static void RewritePropertySetterArgument(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.MutableProperty), MemberType.Getter)]
    public static void RewritePropertyGetterResult(ref int __result) => __result = 42;

    [Postfix]
    [Target(
        typeof(StaticMethodTargets),
        nameof(StaticMethodTargets.GenericIdentity),
        new Type[] { typeof(int) },
        new Type[] { typeof(int) })]
    public static void RewriteClosedGenericResult(ref int __result) => __result = 42;
}

[TestFixture]
public sealed class TargetSelectionTests : PatchTestBase
{
    [Test]
    public void TargetAttributeCanSelectPropertySetter()
    {
        StaticMethodTargets.MutableProperty = 0;
        ApplyPatch(typeof(TargetSelectionPatchMethods), nameof(TargetSelectionPatchMethods.RewritePropertySetterArgument));

        StaticMethodTargets.MutableProperty = 1;

        Assert.That(StaticMethodTargets.MutableProperty, Is.EqualTo(42));
    }

    [Test]
    public void TargetAttributeCanSelectPropertyGetter()
    {
        StaticMethodTargets.MutableProperty = 1;
        ApplyPatch(typeof(TargetSelectionPatchMethods), nameof(TargetSelectionPatchMethods.RewritePropertyGetterResult));

        Assert.That(StaticMethodTargets.MutableProperty, Is.EqualTo(42));
    }

    [Test]
    public void TargetAttributeCanSelectClosedGenericMethod()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(typeof(TargetSelectionPatchMethods), nameof(TargetSelectionPatchMethods.RewriteClosedGenericResult)));
    }
}
