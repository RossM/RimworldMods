namespace Yobai;

internal static class PolyculeHelpers
{
    public static bool AreLovePartnersWillingToShareBed(Pawn first, Pawn second)
    {
        return first != second && !first.Dead && !second.Dead
               && LovePartnerRelationUtility.LovePartnerRelationExists(first, second)
               && BedUtility.WillingToShareBed(first, second);
    }

    public static IEnumerable<Pawn> GetLovePartnersWillingToShareBed(Pawn pawn)
    {
        return pawn.relations.DirectRelations.Where(relation => AreLovePartnersWillingToShareBed(pawn, relation.otherPawn))
            .Select(relation => relation.otherPawn);
    }
}
