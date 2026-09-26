// ReSharper disable MemberCanBeMadeStatic.Global

namespace XylXenos;

[StaticConstructorOnStartup]
public static class StaticEventHandlers
{
    private class Listener : IEventListener
    {
        private void Notify_PawnGenerationEarly(Thing? thing, PawnGenerationData data)
        {
            if (thing is not Pawn pawn)
                return;
            ModifyGenderByGenes(pawn, data.request, data.xenotype);
        }

        public void RegisterWith(EventManager manager)
        {
            manager.Register<PawnGenerationData>(EventDefOf.PreGeneratePawnBioAndName, null, Notify_PawnGenerationEarly,
                priority: -100);
        }

        public void PreUnregister(EventManager manager) { }
    }

    private static readonly Listener listener = new();

    static StaticEventHandlers()
    {
        EventManager.AddStaticListener(listener);
    }

    public static void ModifyGenderByGenes(Pawn pawn, PawnGenerationRequest request, XenotypeDef? xenotype)
    {
        if (request.FixedGender != null)
            return;

        GeneDef? gene = request.ForcedXenogenes?.FirstOrDefault(def => HasGenderRatio(def, GeneType.Xenogene)) ??
                        request.ForcedEndogenes?.FirstOrDefault(def => HasGenderRatio(def, GeneType.Endogene)) ??
                        request.ForcedCustomXenotype?.genes.FirstOrDefault(def => HasGenderRatio(def, request.ForcedCustomXenotype.GeneType)) ??
                        xenotype?.AllGenes.FirstOrDefault(def => HasGenderRatio(def, xenotype.GeneType));
        var comp = gene?.CompProps<GeneCompProperties_GenderRatio>();
        if (comp == null)
            return;

        pawn.gender = Rand.Chance(comp.femaleChance) ? Gender.Female : Gender.Male;
        return;

        static bool HasGenderRatio(GeneDef geneDef, GeneType geneType) =>
            geneDef.Extension_GeneWithComps is not null &&
            geneDef.CompProps<GeneCompProperties_GenderRatio>() != null && 
            (geneDef.Extension_GeneWithComps.geneType is null || geneDef.Extension_GeneWithComps.geneType == geneType);
    }
}
