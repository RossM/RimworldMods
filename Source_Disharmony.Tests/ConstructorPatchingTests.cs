namespace Disharmony.Tests;

public static class ConstructorPatchingPatches
{
    public static int ExecutionCount;
    public static int ParameterObserved;
    public static ConstructorTargets? InstanceObserved;
    public static ConstructorTargets? ResultObserved;

    [Prefix]
    [Target(typeof(ConstructorTargets), memberType: MemberType.Constructor, parameterTypes: [])]
    public static void Prefix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;

    [Postfix]
    [Target(typeof(ConstructorTargets), memberType: MemberType.Constructor, parameterTypes: [])]
    public static void Postfix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;

    [InnerPrefix(
        typeof(ConstructorTargets),
        memberType: MemberType.Constructor,
        parameterTypes: [])]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create), parameterTypes: [])]
    public static void InnerPrefix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;

    [InnerPostfix(
        typeof(ConstructorTargets),
        memberType: MemberType.Constructor,
        parameterTypes: [])]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create), parameterTypes: [])]
    public static void InnerPostfix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;

    [Prefix]
    [Target(
        typeof(ConstructorTargets),
        memberType: MemberType.Constructor,
        parameterTypes: [typeof(int)])]
    public static void Prefix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue(int value) => ParameterObserved = value;

    [Postfix]
    [Target(
        typeof(ConstructorTargets),
        memberType: MemberType.Constructor,
        parameterTypes: [typeof(int)])]
    public static void Postfix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue(int value) => ParameterObserved = value;

    [InnerPrefix(
        typeof(ConstructorTargets),
        memberType: MemberType.Constructor,
        parameterTypes: [typeof(int)])]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create), parameterTypes: [typeof(int)])]
    public static void InnerPrefix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue(int value) => ParameterObserved = value;

    [InnerPostfix(
        typeof(ConstructorTargets),
        memberType: MemberType.Constructor,
        parameterTypes: [typeof(int)])]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create), parameterTypes: [typeof(int)])]
    public static void InnerPostfix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue(int value) => ParameterObserved = value;

    [Prefix]
    [Target(typeof(ConstructorTargets), memberType: MemberType.Constructor, parameterTypes: [])]
    public static void Prefix_Constructor_ReferenceType_Parameterless_Instance_ReadByValue(ConstructorTargets __instance) =>
        InstanceObserved = __instance;

    [Postfix]
    [Target(typeof(ConstructorTargets), memberType: MemberType.Constructor, parameterTypes: [])]
    public static void Postfix_Constructor_ReferenceType_Parameterless_Instance_ReadByValue(ConstructorTargets __instance) =>
        InstanceObserved = __instance;

    [InnerPrefix(
        typeof(ConstructorTargets),
        memberType: MemberType.Constructor,
        parameterTypes: [])]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create), parameterTypes: [])]
    public static void InnerPrefix_Constructor_ReferenceType_Parameterless_Result_ReadByValue(ConstructorTargets? __result) =>
        ResultObserved = __result;

    [InnerPostfix(
        typeof(ConstructorTargets),
        memberType: MemberType.Constructor,
        parameterTypes: [])]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create), parameterTypes: [])]
    public static void InnerPostfix_Constructor_ReferenceType_Parameterless_Result_ReadByValue(ConstructorTargets __result) =>
        ResultObserved = __result;
}

[TestFixture]
public sealed class ConstructorPatchingTests : PatchTestBase
{
    [Test]
    public void Prefix_Constructor_ReferenceType_Parameterless_Executes()
    {
        ConstructorPatchingPatches.ExecutionCount = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.Prefix_Constructor_ReferenceType_Parameterless_Executes));

        var result = new ConstructorTargets();

        Assert.That(ConstructorPatchingPatches.ExecutionCount, Is.EqualTo(1));
        Assert.That(result.ConstructorExecuted, Is.True);
    }

    [Test]
    public void Postfix_Constructor_ReferenceType_Parameterless_Executes()
    {
        ConstructorPatchingPatches.ExecutionCount = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.Postfix_Constructor_ReferenceType_Parameterless_Executes));

        var result = new ConstructorTargets();

        Assert.That(ConstructorPatchingPatches.ExecutionCount, Is.EqualTo(1));
        Assert.That(result.ConstructorExecuted, Is.True);
    }

    [Test]
    public void InnerPrefix_Constructor_ReferenceType_Parameterless_Executes()
    {
        ConstructorPatchingPatches.ExecutionCount = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.InnerPrefix_Constructor_ReferenceType_Parameterless_Executes));

        ConstructorTargets result = ConstructorTargets.Create();

        Assert.That(ConstructorPatchingPatches.ExecutionCount, Is.EqualTo(1));
        Assert.That(result.ConstructorExecuted, Is.True);
    }

    [Test]
    public void InnerPostfix_Constructor_ReferenceType_Parameterless_Executes()
    {
        ConstructorPatchingPatches.ExecutionCount = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.InnerPostfix_Constructor_ReferenceType_Parameterless_Executes));

        ConstructorTargets result = ConstructorTargets.Create();

        Assert.That(ConstructorPatchingPatches.ExecutionCount, Is.EqualTo(1));
        Assert.That(result.ConstructorExecuted, Is.True);
    }

    [Test]
    public void Prefix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue()
    {
        ConstructorPatchingPatches.ParameterObserved = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.Prefix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue));

        var result = new ConstructorTargets(42);

        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(ConstructorPatchingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue()
    {
        ConstructorPatchingPatches.ParameterObserved = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.Postfix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue));

        var result = new ConstructorTargets(42);

        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(ConstructorPatchingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue()
    {
        ConstructorPatchingPatches.ParameterObserved = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.InnerPrefix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue));

        ConstructorTargets result = ConstructorTargets.Create(42);

        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(ConstructorPatchingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue()
    {
        ConstructorPatchingPatches.ParameterObserved = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.InnerPostfix_Constructor_ReferenceType_Parameter_Primitive_ReadByValue));

        ConstructorTargets result = ConstructorTargets.Create(42);

        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(ConstructorPatchingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_Constructor_ReferenceType_Parameterless_Instance_ReadByValue()
    {
        ConstructorPatchingPatches.InstanceObserved = null;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.Prefix_Constructor_ReferenceType_Parameterless_Instance_ReadByValue));

        var result = new ConstructorTargets();

        Assert.That(ConstructorPatchingPatches.InstanceObserved, Is.SameAs(result));
    }

    [Test]
    public void Postfix_Constructor_ReferenceType_Parameterless_Instance_ReadByValue()
    {
        ConstructorPatchingPatches.InstanceObserved = null;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.Postfix_Constructor_ReferenceType_Parameterless_Instance_ReadByValue));

        var result = new ConstructorTargets();

        Assert.That(ConstructorPatchingPatches.InstanceObserved, Is.SameAs(result));
    }

    [Test]
    public void InnerPrefix_Constructor_ReferenceType_Parameterless_Result_ReadByValue()
    {
        ConstructorPatchingPatches.ResultObserved = new ConstructorTargets();
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.InnerPrefix_Constructor_ReferenceType_Parameterless_Result_ReadByValue));

        ConstructorTargets result = ConstructorTargets.Create();

        Assert.That(result.ConstructorExecuted, Is.True);
        Assert.That(ConstructorPatchingPatches.ResultObserved, Is.Null);
    }

    [Test]
    public void InnerPostfix_Constructor_ReferenceType_Parameterless_Result_ReadByValue()
    {
        ConstructorPatchingPatches.ResultObserved = null;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.InnerPostfix_Constructor_ReferenceType_Parameterless_Result_ReadByValue));

        ConstructorTargets result = ConstructorTargets.Create();

        Assert.That(result.ConstructorExecuted, Is.True);
        Assert.That(ConstructorPatchingPatches.ResultObserved, Is.SameAs(result));
    }
}
