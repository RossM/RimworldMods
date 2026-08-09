namespace Disharmony.RuleEngine;

internal abstract record LocalTracker
{
    public abstract int Index { get; }

    public static LocalTracker From(CodeInstruction instruction)
    {
        return instruction.operand is LocalBuilder builder
            ? new LocalTrackerBuilder(builder)
            : new LocalTrackerIndex(instruction.LocalIndex());
    }

    public static int IndexFrom(CodeInstruction instruction)
    {
        return instruction.operand is LocalBuilder builder
            ? builder.LocalIndex
            : instruction.LocalIndex();
    }

    public abstract CodeInstruction Store();

    public abstract CodeInstruction Load(bool useAddress = false);
}

internal record LocalTrackerBuilder(LocalBuilder Builder) : LocalTracker
{
    public override int Index => Builder.LocalIndex;
    public Type Type => Builder.LocalType;

    public override CodeInstruction Store() =>
        new(Index <= byte.MaxValue ? OpCodes.Stloc_S : OpCodes.Stloc, Builder);

    public override CodeInstruction Load(bool useAddress = false) => useAddress
        ? new(Index <= byte.MaxValue ? OpCodes.Ldloca_S : OpCodes.Ldloca, Builder)
        : new(Index <= byte.MaxValue ? OpCodes.Ldloc_S : OpCodes.Ldloc, Builder);
}

internal record LocalTrackerIndex(int Index) : LocalTracker
{
    public override int Index { get; } = Index;

    public override CodeInstruction Store() => CodeInstruction.StoreLocal(Index);

    public override CodeInstruction Load(bool useAddress = false) => CodeInstruction.LoadLocal(Index, useAddress);
}
