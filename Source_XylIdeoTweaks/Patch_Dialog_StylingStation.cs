using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using UnityEngine;
using Verse;
using Color = UnityEngine.Color;

namespace Source_XylIdeoTweaks;

[HarmonyPatch(typeof(Dialog_StylingStation))]
public static class Patch_Dialog_StylingStation
{
    [InfixPostfix(typeof(Widgets), nameof(Widgets.ColorSelector))]
    [InfixPatch("DrawApparelColor")]
    public static void ColorSelector_Postfix(Pawn ___pawn, ref bool __result)
    {
        if (__result)
            PawnData.Get(___pawn).autoColorMode = AutoColorMode.NoAutoColor;
    }

    [HarmonyPrefix]
    [HarmonyPatch("DrawPawn")]
    public static void DrawPawn_Prefix(Pawn ___pawn, Dictionary<Apparel, Color> ___apparelColors, ref Rect rect)
    {
        Rect extraLine = rect;
        extraLine.yMin = rect.yMax - Text.LineHeight;
        rect.yMax = extraLine.yMin - 3f;

        var pawnData = PawnData.Get(___pawn);

        if (Widgets.ButtonText(extraLine, TextForMode(pawnData.autoColorMode)))
        {
            List<FloatMenuOption> options = [];

            foreach (var value in (AutoColorMode[])Enum.GetValues(typeof(AutoColorMode)))
            {
                var localValue = value;
                options.Add(new(TextForMode(value), () =>
                {
                    pawnData.autoColorMode = localValue;
                    if (localValue == AutoColorMode.NoAutoColor)
                        ResetColors(___pawn, ___apparelColors);
                    else
                        ApplyColors(___pawn, ___apparelColors);
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("PostOpen")]
    public static void PostOpen_Postfix(Pawn ___pawn, Dictionary<Apparel, Color> ___apparelColors)
    {
        if (PawnData.Get(___pawn).autoColorMode != AutoColorMode.NoAutoColor)
            ApplyColors(___pawn, ___apparelColors);
    }

    private static void ApplyColors(Pawn pawn, Dictionary<Apparel, Color> apparelColors)
    {
        if (PatchHelpers.AutoColorColor(pawn) is not { } color)
            return;

        foreach (var item in pawn.apparel.WornApparel)
        {
            if (item.TryGetComp<CompColorable>() != null)
                apparelColors[item] = color;
        }
    }

    private static void ResetColors(Pawn pawn, Dictionary<Apparel, Color> apparelColors)
    {
        foreach (var item in pawn.apparel.WornApparel)
        {
            if (item.TryGetComp<CompColorable>() != null)
                apparelColors[item] = item.DesiredColor ?? item.GetColorIgnoringTainted();
        }
    }

    public static string TextForMode(AutoColorMode mode) => mode switch
    {
        AutoColorMode.NoAutoColor => "Auto-color off",
        AutoColorMode.UseFavoriteColor => "Auto favorite color",
        AutoColorMode.UseIdeoligeonColor => "Auto ideoligeon color",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };
}
