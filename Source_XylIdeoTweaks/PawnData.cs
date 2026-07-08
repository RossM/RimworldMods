using System;

namespace XylIdeos;

public enum AutoColorMode
{
    NoAutoColor,
    UseFavoriteColor,
    UseIdeoligeonColor,
}

[ScribeLabel("Source_XylIdeoTweaks.PawnData")]
public class PawnData : IPawnData, IExposable
{
    public Pawn Pawn
    {
        get => field ?? throw new InvalidOperationException();
        set;
    }

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
    public void Init()
    {
    }
}
