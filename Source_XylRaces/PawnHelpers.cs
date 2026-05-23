using System.Linq;
using Verse;
using XylXenos.Genes;

namespace XylXenos;

public static class PawnHelpers
{
    public static float? RaceManhunterOnDamageChance(this Pawn pawn)
    {
        return pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_WildMan>()
            .FirstOrDefault(e => e.manhunterOnDamageChance != null)?
            .manhunterOnDamageChance;
    }

    public static float? RaceManhunterOnTameFailChance(this Pawn pawn)
    {
        return pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_WildMan>()
            .FirstOrDefault(e => e.manhunterOnTameFailChance != null)?
            .manhunterOnTameFailChance;
    }
}
