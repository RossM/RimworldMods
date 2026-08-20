namespace XylXenos.Patches;

[HarmonyPatch(typeof(MemoryThoughtHandler))]
public static class Patch_MemoryThoughtHandler
{
    [Feature(typeof(DefModExtension_Thought))]
    [Postfix]
    [Target(nameof(MemoryThoughtHandler.RemoveMemoriesOfDefIf))]
    public static void RemoveMemoriesOfDefIf_Postfix(MemoryThoughtHandler __instance, ThoughtDef def, Func<Thought_Memory, bool> predicate)
    {
        var extension = def.GetModExtension<DefModExtension_Thought>();
        if (extension?.extraThoughts == null)
            return;
        foreach (var extraDef in extension.extraThoughts)
            __instance.RemoveMemoriesOfDefIf(extraDef, predicate);
    }

    [Feature(typeof(DefModExtension_Thought))]
    [Postfix]
    [Target(nameof(MemoryThoughtHandler.RemoveMemoriesOfDefWhereOtherPawnIs))]
    public static void RemoveMemoriesOfDefWhereOtherPawnIs_Postfix(MemoryThoughtHandler __instance, ThoughtDef def, Pawn otherPawn)
    {
        var extension = def.GetModExtension<DefModExtension_Thought>();
        if (extension?.extraThoughts == null)
            return;
        foreach (var extraDef in extension.extraThoughts)
            __instance.RemoveMemoriesOfDefWhereOtherPawnIs(extraDef, otherPawn);
    }

    [Feature(typeof(DefModExtension_Thought))]
    [Postfix]
    [Target(nameof(MemoryThoughtHandler.RemoveMemoriesOfDef))]
    public static void RemoveMemoriesOfDef_Postfix(MemoryThoughtHandler __instance, ThoughtDef def)
    {
        var extension = def.GetModExtension<DefModExtension_Thought>();
        if (extension?.extraThoughts == null)
            return;
        foreach (var extraDef in extension.extraThoughts)
            __instance.RemoveMemoriesOfDef(extraDef);
    }

    [Feature(typeof(DefModExtension_Thought))]
    [Postfix]
    [Target(nameof(MemoryThoughtHandler.TryGainMemory), typeof(Thought_Memory), typeof(Pawn))]
    public static void TryGainMemory_Postfix(MemoryThoughtHandler __instance, Thought_Memory newThought, Pawn otherPawn)
    {
        var extension = newThought.def.GetModExtension<DefModExtension_Thought>();
        if (extension?.extraThoughts == null)
            return;
        foreach (var extraDef in extension.extraThoughts)
        {
            if (extraDef.stages[newThought.CurStageIndex] != null)
                __instance.TryGainMemory(ThoughtMaker.MakeThought(extraDef, newThought.CurStageIndex), otherPawn);
        }
    }
}
