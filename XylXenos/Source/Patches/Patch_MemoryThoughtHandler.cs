namespace XylXenos.Patches;

[Patch(typeof(MemoryThoughtHandler))]
public static class Patch_MemoryThoughtHandler
{
    [Feature(typeof(DefModExtension_Thought))]
    [Postfix]
    [Target(nameof(MemoryThoughtHandler.RemoveMemoriesOfDefIf))]
    public static void RemoveMemoriesOfDefIf_Postfix(MemoryThoughtHandler __instance, ThoughtDef def, Func<Thought_Memory, bool> predicate)
    {
        if (def.GetModExtension<DefModExtension_Thought>()?.extraThoughts is not { } extraThoughts)
            return;
        foreach (var extraDef in extraThoughts)
            __instance.RemoveMemoriesOfDefIf(extraDef, predicate);
    }

    [Feature(typeof(DefModExtension_Thought))]
    [Postfix]
    [Target(nameof(MemoryThoughtHandler.RemoveMemoriesOfDefWhereOtherPawnIs))]
    public static void RemoveMemoriesOfDefWhereOtherPawnIs_Postfix(MemoryThoughtHandler __instance, ThoughtDef def, Pawn otherPawn)
    {
        if (def.GetModExtension<DefModExtension_Thought>()?.extraThoughts is not { } extraThoughts)
            return;
        foreach (var extraDef in extraThoughts)
            __instance.RemoveMemoriesOfDefWhereOtherPawnIs(extraDef, otherPawn);
    }

    [Feature(typeof(DefModExtension_Thought))]
    [Postfix]
    [Target(nameof(MemoryThoughtHandler.RemoveMemoriesOfDef))]
    public static void RemoveMemoriesOfDef_Postfix(MemoryThoughtHandler __instance, ThoughtDef def)
    {
        if (def.GetModExtension<DefModExtension_Thought>()?.extraThoughts is not { } extraThoughts)
            return;
        foreach (var extraDef in extraThoughts)
            __instance.RemoveMemoriesOfDef(extraDef);
    }

    [Feature(typeof(DefModExtension_Thought))]
    [Postfix]
    [Target(nameof(MemoryThoughtHandler.TryGainMemory), typeof(Thought_Memory), typeof(Pawn))]
    public static void TryGainMemory_Postfix(MemoryThoughtHandler __instance, Thought_Memory newThought, Pawn otherPawn)
    {
        if (newThought.def.GetModExtension<DefModExtension_Thought>()?.extraThoughts is not { } extraThoughts)
            return;
        foreach (var extraDef in extraThoughts)
        {
            if (extraDef.stages[newThought.CurStageIndex] is not null)
                __instance.TryGainMemory(ThoughtMaker.MakeThought(extraDef, newThought.CurStageIndex), otherPawn);
        }
    }
}
