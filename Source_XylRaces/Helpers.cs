using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public readonly struct ProfileBlock : IDisposable
    {
#if DEBUG
        public const bool GlobalEnabled = true;
#else
        public const bool GlobalEnabled = false;
#endif
        public static bool InstrumentTickManager = false;
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

        [DebugAction("Toggle tick profiling"), UsedImplicitly]
        public static void ToggleTickProfiling()
        {
            InstrumentTickManager = !InstrumentTickManager;
        }
    }

    public static class Helpers
    {
        public static IEnumerable<T> EverythingOfType<T>(this Pawn pawn) where T : class
        {
            foreach (T gene in pawn.ActiveGenesOfType<T>())
                yield return gene;
            foreach (T geneDefExt in pawn.ActiveGeneDefExtensionsOfType<T>())
                yield return geneDefExt;
            foreach (T hediff in pawn.HediffsOfType<T>())
                yield return hediff;
            foreach (T hediffDefExt in pawn.HediffsWithModExtension<T>().SelectMany(h => h.def.modExtensions.OfType<T>()))
                yield return hediffDefExt;
            foreach (T hediffComp in pawn.HediffsWithComp<T>().SelectMany(h => h.comps.OfType<T>()))
                yield return hediffComp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable ?? Enumerable.Empty<T>();
        }

        public static float GetStatBase(this ThingDef thingDef, StatDef statDef)
        {
            return thingDef.statBases.FirstOrDefault(s => s.stat == statDef)?.value ?? 0;
        }

        public static IEnumerable<Pawn> GetPawns(this Faction faction)
        {
            return Find.Maps.SelectMany(map => map.mapPawns.PawnsInFaction(faction));
        }
    }
}