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
        foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
        {
            if (AreLovePartnersWillingToShareBed(pawn, relation.otherPawn))
                yield return relation.otherPawn;
        }
    }

    public static HashSet<Pawn> GetPolycule(Pawn pawn)
    {
        HashSet<Pawn> members = [pawn];
        Stack<Pawn> pending = new();
        pending.Push(pawn);
        while (pending.Count > 0)
        {
            foreach (Pawn partner in GetLovePartnersWillingToShareBed(pending.Pop()))
            {
                if (members.Add(partner))
                    pending.Push(partner);
            }
        }

        return members;
    }
}
