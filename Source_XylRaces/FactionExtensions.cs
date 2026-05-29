namespace XylXenos;

public static class FactionExtensions
{
    extension(Faction faction)
    {
        public IEnumerable<Pawn> AllPawns => Find.Maps.SelectMany(map => map.mapPawns.PawnsInFaction(faction));
    }
}