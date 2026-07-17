using System;
using UnityEngine;
using Color = UnityEngine.Color;

namespace XylIdeos;

[HarmonyPatch(typeof(Dialog_StylingStation))]
public static class Patch_Dialog_StylingStation
{
    [Feature(Features.AutoColorApparel)]
    [InnerPostfix(typeof(Widgets), nameof(Widgets.ColorSelector))]
    [Target("DrawApparelColor")]
    public static void ColorSelector_Postfix(Pawn ___pawn, ref bool __result)
    {
        if (__result)
            PawnData.Get(___pawn).autoColorMode = AutoColorMode.NoAutoColor;
    }

    [Feature(Features.AutoColorApparel)]
    [Prefix]
    [Target("DrawPawn")]
    public static void DrawPawn_Prefix(Pawn ___pawn, Dictionary<Apparel, Color> ___apparelColors, ref Rect rect)
    {
        Rect extraLine = rect;
        extraLine.yMin = rect.yMax - Text.LineHeight;
        rect.yMax = extraLine.yMin - 3f;

        var pawnData = PawnData.Get(___pawn);

        Widgets.Label(extraLine.LeftHalf(), "Auto-color");

        if (Widgets.ButtonText(extraLine.RightHalf(), TextForMode(pawnData.autoColorMode)))
        {
            List<FloatMenuOption> options = [];

            foreach (var value in Enum.GetValues<AutoColorMode>())
            {
                if (value == AutoColorMode.UseIdeoligeonColor && (___pawn.ideo == null || Find.IdeoManager.classicMode))
                    continue;

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

    [Feature(Features.AutoColorApparel)]
    [Postfix]
    [Target("PostOpen")]
    public static void PostOpen_Postfix(Pawn ___pawn, Dictionary<Apparel, Color> ___apparelColors)
    {
        if (PawnData.Get(___pawn).autoColorMode != AutoColorMode.NoAutoColor)
            ApplyColors(___pawn, ___apparelColors);
    }

    [Feature(Features.AutoColorApparel)]
    [Postfix]
    [Target("Reset")]
    public static void Reset_Postfix(Pawn ___pawn, Dictionary<Apparel, Color> ___apparelColors)
    {
        if (PatchHelpers.AutoColorColor(___pawn) is not { } color)
            return;

        if (___apparelColors.Values.Any(apparelColor => !color.IndistinguishableFrom(apparelColor)))
            PawnData.Get(___pawn).autoColorMode = AutoColorMode.NoAutoColor;
    }

    private static void ApplyColors(Pawn pawn, Dictionary<Apparel, Color> apparelColors)
    {
        if (PatchHelpers.AutoColorColor(pawn) is not { } color)
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

    private static void ResetColors(Pawn pawn, Dictionary<Apparel, Color> apparelColors)
    {
        foreach (var item in pawn.apparel.WornApparel)
        {
            if (item.TryGetComp<CompColorable>() != null)
                apparelColors[item] = item.DesiredColor ?? item.GetColorIgnoringTainted();
        }
    }

    private static string TextForMode(AutoColorMode mode) => mode switch
    {
        AutoColorMode.NoAutoColor => "Off",
        AutoColorMode.UseFavoriteColor => "Favorite color",
        AutoColorMode.UseIdeoligeonColor => "Ideoligeon color",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };
}
