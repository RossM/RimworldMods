namespace Disharmony.Tests;

public static class ConstructorPatchingPatches
{
    public static int ExecutionCount;

    [Prefix]
    [Target(typeof(ConstructorTargets), ".ctor")]
    public static void Prefix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;

    [Postfix]
    [Target(typeof(ConstructorTargets), ".ctor")]
    public static void Postfix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;

    [InnerPrefix(typeof(ConstructorTargets), ".ctor")]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create))]
    public static void InnerPrefix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;

    [InnerPostfix(typeof(ConstructorTargets), ".ctor")]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create))]
    public static void InnerPostfix_Constructor_ReferenceType_Parameterless_Executes() => ExecutionCount++;
}

[TestFixture]
public sealed class ConstructorPatchingTests : PatchTestBase
{
    [Test]
    [Ignore("Patching constructors is not implemented")]
    public void Prefix_Constructor_ReferenceType_Parameterless_Executes()
    {
        ConstructorPatchingPatches.ExecutionCount = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.Prefix_Constructor_ReferenceType_Parameterless_Executes));

        _ = new ConstructorTargets();

        Assert.That(ConstructorPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test]
    [Ignore("Patching constructors is not implemented")]
    public void Postfix_Constructor_ReferenceType_Parameterless_Executes()
    {
        ConstructorPatchingPatches.ExecutionCount = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.Postfix_Constructor_ReferenceType_Parameterless_Executes));

        _ = new ConstructorTargets();

        Assert.That(ConstructorPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test]
    [Ignore("Patching constructors is not implemented")]
    public void InnerPrefix_Constructor_ReferenceType_Parameterless_Executes()
    {
        ConstructorPatchingPatches.ExecutionCount = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.InnerPrefix_Constructor_ReferenceType_Parameterless_Executes));

        _ = ConstructorTargets.Create();

        Assert.That(ConstructorPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test]
    [Ignore("Patching constructors is not implemented")]
    public void InnerPostfix_Constructor_ReferenceType_Parameterless_Executes()
    {
        ConstructorPatchingPatches.ExecutionCount = 0;
        ApplyPatch(
            typeof(ConstructorPatchingPatches),
            nameof(ConstructorPatchingPatches.InnerPostfix_Constructor_ReferenceType_Parameterless_Executes));

        _ = ConstructorTargets.Create();

        Assert.That(ConstructorPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }
}
