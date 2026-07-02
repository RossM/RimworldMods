using UnityEngine;

namespace Source_XylIdeoTweaks;

public static class PatchHelpers
{
    public static Color? AutoColorColor(Pawn pawn) =>
        PawnData.Get(pawn).autoColorMode switch
        {
            AutoColorMode.UseFavoriteColor => pawn.story?.favoriteColor.color,
            AutoColorMode.UseIdeoligeonColor => pawn.Ideo?.ApparelColor,
            _ => null
        };
}
