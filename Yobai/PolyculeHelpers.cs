namespace Yobai;

internal static class PolyculeHelpers
{
    public static bool AreBedSharingPartners(Pawn first, Pawn second)
    {
        return first != second && !first.Dead && !second.Dead
               && LovePartnerRelationUtility.LovePartnerRelationExists(first, second)
               && BedUtility.WillingToShareBed(first, second);
    }

    public static IEnumerable<Pawn> GetBedSharingPartners(Pawn pawn)
    {
        foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
        {
            if (LovePartnerRelationUtility.IsLovePartnerRelation(relation.def)
                && AreBedSharingPartners(pawn, relation.otherPawn))
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
            foreach (Pawn partner in GetBedSharingPartners(pending.Pop()))
            {
                if (members.Add(partner))
                    pending.Push(partner);
            }
        }

        return members;
    }
}
