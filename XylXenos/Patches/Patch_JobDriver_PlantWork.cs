namespace XylXenos.Patches;

[Patch(typeof(JobDriver_PlantWork))]
public static class Patch_JobDriver_PlantWork
{
    [Feature(nameof(DefOf.XylTreeCuttingSpeed))]
    [Postfix]
    [Target(nameof(JobDriver_PlantWork.WorkDonePerTick))]
    public static void WorkDonePerTick_Postfix(Pawn actor, Plant plant, ref float __result)
    {
        if (plant.def.plant.IsTree)
            __result *= actor.GetStatValue(DefOf.XylTreeCuttingSpeed);
    }
}
