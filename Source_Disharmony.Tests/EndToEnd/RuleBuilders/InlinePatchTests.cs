namespace Disharmony.Tests.EndToEnd.RuleBuilders;

[TestFixture]
[Timeout(5000)]
public sealed class InlinePatchTests : PatchTestBase
{
    [Test]
    public void InlinePrefixExecutesInlinedBranchAndRefWrite()
    {
        MethodInfo patch = typeof(InlinePatchPatches)
            .GetMethod(nameof(InlinePatchPatches.InlinePrefixExecutesInlinedBranchAndRefWrite))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.IntIdentity))!;
        Patcher.Patch(
            patch,
            PatchType.Prefix,
            options: PatchOptions.Inline,
            targets: [target]);

        int negativeResult = StaticMethodTargets.IntIdentity(-42);
        int positiveResult = StaticMethodTargets.IntIdentity(7);

        Assert.That(negativeResult, Is.EqualTo(42));
        Assert.That(positiveResult, Is.EqualTo(7));
    }

    [Test]
    public void InlinePrefix_ArgumentOpcodes_LdargS_LdargaS_StargS()
    {
        InlinePatchPatches.ArgumentLoaded = 0;
        InlinePatchPatches.ArgumentAddressed = 0;
        InlinePatchPatches.ArgumentStored = 0;
        MethodInfo patch = typeof(InlinePatchPatches)
            .GetMethod(nameof(InlinePatchPatches.InlinePrefix_ArgumentOpcodes_LdargS_LdargaS_StargS))!;
        List<CodeInstruction> instructions = PatchProcessor.GetOriginalInstructions(patch);
        Assert.That(instructions.Any(instruction => instruction.opcode == OpCodes.Ldarg_S), Is.True);
        Assert.That(instructions.Any(instruction => instruction.opcode == OpCodes.Ldarga_S), Is.True);
        Assert.That(instructions.Any(instruction => instruction.opcode == OpCodes.Starg_S), Is.True);
        MethodInfo target = typeof(InlinePatchTargets)
            .GetMethod(nameof(InlinePatchTargets.FiveArguments))!;
        Patcher.Patch(
            patch,
            PatchType.Prefix,
            options: PatchOptions.Inline,
            targets: [target]);

        InlinePatchTargets.FiveArguments(1, 2, 3, 4, 5);

        Assert.That(InlinePatchPatches.ArgumentLoaded, Is.EqualTo(5));
        Assert.That(InlinePatchPatches.ArgumentAddressed, Is.EqualTo(6));
        Assert.That(InlinePatchPatches.ArgumentStored, Is.EqualTo(42));
    }

    [Test]
    public void InlinePrefix_LocalOpcodes_LdlocS_Stloc0_StlocS()
    {
        InlinePatchPatches.Local0Observed = 0;
        InlinePatchPatches.Local4Observed = 0;
        MethodInfo patch = typeof(InlinePatchPatches)
            .GetMethod(nameof(InlinePatchPatches.InlinePrefix_LocalOpcodes_LdlocS_Stloc0_StlocS))!;
        List<CodeInstruction> instructions = PatchProcessor.GetOriginalInstructions(patch);
        Assert.That(instructions.Any(instruction => instruction.opcode == OpCodes.Stloc_0), Is.True);
        Assert.That(instructions.Any(instruction => instruction.opcode == OpCodes.Stloc_S), Is.True);
        Assert.That(instructions.Any(instruction => instruction.opcode == OpCodes.Ldloc_S), Is.True);
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;
        Patcher.Patch(
            patch,
            PatchType.Prefix,
            options: PatchOptions.Inline,
            targets: [target]);

        StaticMethodTargets.Void();

        Assert.That(InlinePatchPatches.Local0Observed, Is.EqualTo(10));
        Assert.That(InlinePatchPatches.Local4Observed, Is.EqualTo(14));
    }

    [Test]
    public void InlinePrefix_SwitchOpcode()
    {
        InlinePatchPatches.SwitchObserved = 0;
        MethodInfo patch = typeof(InlinePatchPatches)
            .GetMethod(nameof(InlinePatchPatches.InlinePrefix_SwitchOpcode))!;
        List<CodeInstruction> instructions = PatchProcessor.GetOriginalInstructions(patch);
        Assert.That(instructions.Any(instruction => instruction.opcode == OpCodes.Switch), Is.True);
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.IntIdentity))!;
        Patcher.Patch(
            patch,
            PatchType.Prefix,
            options: PatchOptions.Inline,
            targets: [target]);

        Assert.That(StaticMethodTargets.IntIdentity(0), Is.EqualTo(0));
        Assert.That(InlinePatchPatches.SwitchObserved, Is.EqualTo(10));
        Assert.That(StaticMethodTargets.IntIdentity(1), Is.EqualTo(1));
        Assert.That(InlinePatchPatches.SwitchObserved, Is.EqualTo(11));
        Assert.That(StaticMethodTargets.IntIdentity(2), Is.EqualTo(2));
        Assert.That(InlinePatchPatches.SwitchObserved, Is.EqualTo(12));
        Assert.That(StaticMethodTargets.IntIdentity(3), Is.EqualTo(3));
        Assert.That(InlinePatchPatches.SwitchObserved, Is.EqualTo(13));
        Assert.That(StaticMethodTargets.IntIdentity(4), Is.EqualTo(4));
        Assert.That(InlinePatchPatches.SwitchObserved, Is.EqualTo(14));
        Assert.That(StaticMethodTargets.IntIdentity(5), Is.EqualTo(5));
        Assert.That(InlinePatchPatches.SwitchObserved, Is.EqualTo(99));
    }
}
