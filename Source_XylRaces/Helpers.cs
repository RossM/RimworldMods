using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace XylXenos
{
    public readonly struct ProfileBlock : IDisposable
    {
        public const bool GlobalEnabled = true;
        private readonly bool _enabled;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProfileBlock(bool enabled = GlobalEnabled, [CallerMemberName] string methodName = null)
        {
            _enabled = enabled;
            if (!_enabled)
                return;
            string label = methodName ?? "<Unknown>";

            DeepProfiler.Start(label);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!_enabled)
                return;
            DeepProfiler.End();
        }
    }

    public static class Helpers
    {
        // ReSharper disable once UnusedMember.Global
        public static float GetStatBase(this ThingDef thingDef, StatDef statDef)
        {
            return thingDef.statBases.FirstOrDefault(s => s.stat == statDef)?.value ?? statDef.defaultBaseValue;
        }

        public static IEnumerable<Pawn> GetPawns(this Faction faction)
        {
            return Find.Maps.SelectMany(map => map.mapPawns.PawnsInFaction(faction));
        }

        public static XenotypeDef GetDefaultXenotype(this XenotypeSet xenotypeSet)
        {
            if (xenotypeSet is XenotypeSetWithDefault withDefault)
                return withDefault.defaultXenotype;
            else
                return XenotypeDefOf.Baseliner;
        }
    }
}
