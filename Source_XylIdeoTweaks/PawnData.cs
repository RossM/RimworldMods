using Verse;
using Xylib;

namespace Source_XylIdeoTweaks;

public enum AutoColorMode
{
    NoAutoColor,
    UseFavoriteColor,
    UseIdeoligeonColor,
}

public class PawnData : IPawnData, IExposable
{
    [Unsaved] public Pawn pawn;

    public AutoColorMode autoColorMode;

    public void ExposeData()
    {
        Scribe_Values.Look(ref autoColorMode, nameof(autoColorMode), defaultValue: AutoColorMode.NoAutoColor);
    }

    public static PawnData Get(Pawn pawn)
    {
        return PawnExtraData<PawnData>.Get(pawn);
    }

    // ReSharper disable once ParameterHidesMember
    public void Init(Pawn pawn)
    {
        this.pawn = pawn;
    }
}
