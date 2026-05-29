using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylXenos;

public static class FactionExtensions
{
    extension(Faction faction)
    {
        public IEnumerable<Pawn> AllPawns => Find.Maps.SelectMany(map => map.mapPawns.PawnsInFaction(faction));
    }
}