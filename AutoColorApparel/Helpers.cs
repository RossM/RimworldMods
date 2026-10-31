using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Waylith.Injection;

namespace AutoColorApparel;

public enum AutoColorMode
{
    NoAutoColor,
    UseFavoriteColor,
    UseIdeoligeonColor,
}

internal static class Helpers
{
    extension(Pawn pawn)
    {
        [InjectedField(Default = AutoColorMode.NoAutoColor)]
        public ref AutoColorMode AutoColorMode => throw new NotImplementedException("no injection");

        public Color? AutoColorColor =>
            pawn.AutoColorMode switch
            {
                AutoColorMode.UseFavoriteColor => pawn.story?.favoriteColor.color,
                AutoColorMode.UseIdeoligeonColor => pawn.Ideo?.ApparelColor,
                _ => null,
            };
    }

    public static void ApplyColors(Pawn pawn, Dictionary<Apparel, Color> apparelColors)
    {
        if (pawn.AutoColorColor is not { } color)
            return;
        if (pawn.apparel is null)
            return;

        foreach (var item in pawn.apparel.WornApparel)
        {
            if (item.TryGetComp<CompColorable>() != null && !pawn.apparel.IsLocked(item))
            {
                Color oldColor = item.DesiredColor ?? item.GetColorIgnoringTainted();
                apparelColors[item] = color.IndistinguishableFrom(oldColor) ? oldColor : color;
            }
        }
    }

    public static void ResetColors(Pawn pawn, Dictionary<Apparel, Color> apparelColors)
    {
        if (pawn.apparel is null)
            return;

        foreach (var item in pawn.apparel.WornApparel)
        {
            if (item.TryGetComp<CompColorable>() != null)
                apparelColors[item] = item.DesiredColor ?? item.GetColorIgnoringTainted();
        }
    }

    public static string TextForMode(AutoColorMode mode) => $"AutoColorApparel_Mode_{mode}".Translate();
}
