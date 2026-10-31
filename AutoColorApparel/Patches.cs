using System;
using System.Collections.Generic;
using System.Linq;
using Disharmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoColorApparel;

[Patch]
public static class Patches
{
    [Postfix]
    [Inner(typeof(Widgets), nameof(Widgets.ColorSelector))]
    [Target(typeof(Dialog_StylingStation), "DrawApparelColor")]
    public static void ColorSelector_Postfix(Pawn ___pawn, ref bool __result)
    {
        if (__result)
            ___pawn.AutoColorMode = AutoColorMode.NoAutoColor;
    }

    [Prefix]
    [Target(typeof(Dialog_StylingStation), "DrawPawn")]
    public static void DrawPawn_Prefix(Pawn ___pawn, Dictionary<Apparel, Color> ___apparelColors, ref Rect rect)
    {
        Rect extraLine = rect;
        extraLine.yMin = rect.yMax - Text.LineHeight;
        rect.yMax = extraLine.yMin - 3f;

        Widgets.Label(extraLine.LeftHalf(), "Auto-color");

        if (Widgets.ButtonText(extraLine.RightHalf(), Helpers.TextForMode(___pawn.AutoColorMode)))
        {
            List<FloatMenuOption> options = [];

            foreach (var value in Enum.GetValues<AutoColorMode>())
            {
                if (value == AutoColorMode.UseIdeoligeonColor && (___pawn.ideo == null || Find.IdeoManager.classicMode))
                    continue;

                var localValue = value;
                options.Add(new(Helpers.TextForMode(value), () =>
                {
                    ___pawn.AutoColorMode = localValue;
                    if (localValue == AutoColorMode.NoAutoColor)
                        Helpers.ResetColors(___pawn, ___apparelColors);
                    else
                        Helpers.ApplyColors(___pawn, ___apparelColors);
                }));
            }

            Find.WindowStack!.Add(new FloatMenu(options));
        }
    }

    [Postfix]
    [Target(typeof(Pawn), nameof(Pawn.ExposeData))]
    public static void ExposeData_Postfix(Pawn __instance)
    {
        Scribe.EnterNode("Xylthixlm.AutoColorApparel");
        Scribe_Values.Look(ref __instance.AutoColorMode, "AutoColorMode");
        Scribe.ExitNode();
    }

    [Postfix]
    [Target(typeof(Dialog_StylingStation), "PostOpen")]
    public static void PostOpen_Postfix(Pawn ___pawn, Dictionary<Apparel, Color> ___apparelColors)
    {
        if (___pawn.AutoColorMode != AutoColorMode.NoAutoColor)
            Helpers.ApplyColors(___pawn, ___apparelColors);
    }

    [Postfix]
    [Target(typeof(Dialog_StylingStation), "Reset")]
    public static void Reset_Postfix(Pawn ___pawn, Dictionary<Apparel, Color> ___apparelColors)
    {
        if (___pawn.AutoColorColor is not { } color)
            return;

        if (___apparelColors.Values.Any(apparelColor => !color.IndistinguishableFrom(apparelColor)))
            ___pawn.AutoColorMode = AutoColorMode.NoAutoColor;
    }

    [Prefix]
    [Target(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.TryCreateRecolorJob))]
    public static void TryCreateRecolorJob_Prefix(Pawn pawn)
    {
        if (pawn.AutoColorColor is not { } color)
            return;
        if (pawn.apparel is null)
            return;

        foreach (var item in pawn.apparel.WornApparel)
        {
            if (item.TryGetComp<CompColorable>() != null)
            {
                Color oldColor = item.GetColorIgnoringTainted();
                item.DesiredColor = color.IndistinguishableFrom(oldColor) ? null : color;
            }
        }
    }
}
