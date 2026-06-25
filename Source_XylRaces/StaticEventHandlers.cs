namespace XylXenos;

[StaticConstructorOnStartup]
public static class StaticEventHandlers
{
    public class Listener : IEventListener
    {
        public void Notify_PawnGenerationEarly(Thing thing, PawnGenerationEarlyData data)
        {
            if (thing is not Pawn pawn)
                return;
            ModifyGenderByGenes(pawn, data.request, data.xenotype);
        }

        public void RegisterWith(EventManager manager)
        {
            Log.Message("StaticEventHandlers.Listener.RegisterWith");
            manager.Register<PawnGenerationEarlyData>(EventDefOf.PawnGenerationEarly, null, Notify_PawnGenerationEarly,
                priority: -100);
        }

        public void PreUnregister(EventManager manager)
        {
        }
    }

    public static readonly Listener listener = new();

    static StaticEventHandlers()
    {
        EventManager.AddStaticListener(listener);
    }

    public static void ModifyGenderByGenes(Pawn pawn, PawnGenerationRequest request, XenotypeDef xenotype)
    {
        if (request.FixedGender != null)
            return;

        static bool HasGenderRatio(GeneDef geneDef) => geneDef.DefExt?.CompProps<GeneCompProperties_GenderRatio>() != null;

        GeneDef gene = request.ForcedEndogenes?.FirstOrDefault(HasGenderRatio) ??
                       request.ForcedXenogenes?.FirstOrDefault(HasGenderRatio) ??
                       request.ForcedCustomXenotype?.genes.FirstOrDefault(HasGenderRatio) ??
                       xenotype?.AllGenes.FirstOrDefault(HasGenderRatio);
        var comp = gene?.DefExt!.CompProps<GeneCompProperties_GenderRatio>();
        if (comp == null)
            return;

        pawn.gender = Rand.Chance(comp.femaleChance) ? Gender.Female : Gender.Male;
    }
}
