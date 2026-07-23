namespace Disharmony.Tests;

public static class ConstructorPatchingPatches
{
    public static int ExecutionCount;

    [Prefix]
    [Target(typeof(ConstructorTargets), memberType: MemberType.Constructor)]
    public static void Prefix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;

    [Postfix]
    [Target(typeof(ConstructorTargets), memberType: MemberType.Constructor)]
    public static void Postfix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;

    [InnerPrefix(typeof(ConstructorTargets), memberType: MemberType.Constructor)]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create))]
    public static void InnerPrefix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;

    [InnerPostfix(typeof(ConstructorTargets), memberType: MemberType.Constructor)]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create))]
    public static void InnerPostfix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;
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
}
