namespace Disharmony.Tests;

public static class UnpatchPatches
{
    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetA))]
    public static void PrefixReturningFalseSkipsValueTypeTarget_FirstPostfix(ref int __result) => __result = 42;

    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetB))]
    public static void PrefixReturningFalseSkipsValueTypeTarget_SecondPostfix(ref int __result) => __result = 42;
}

public static class UnpatchPatchTargets
{
    public static int TargetA() => 1;
    public static int TargetB() => 2;
}

internal class UnpatchTests
{
    private static void ApplyPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(UnpatchPatches).GetMethod(patchMethodName));

    [Test]
    public void PrefixReturningFalseSkipsValueTypeTarget()
    {
        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));

        ApplyPatch(nameof(UnpatchPatches.PrefixReturningFalseSkipsValueTypeTarget_FirstPostfix));

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(42));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));

        ApplyPatch(nameof(UnpatchPatches.PrefixReturningFalseSkipsValueTypeTarget_SecondPostfix));

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(42));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(42));

        Autopatcher.UnpatchAll(typeof(UnpatchPatches).Assembly);

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));
    }

}
