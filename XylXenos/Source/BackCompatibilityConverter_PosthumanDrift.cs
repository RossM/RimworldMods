using System.Xml;

namespace XylXenos;

[UsedFromReflection]
internal class BackCompatibilityConverter_PosthumanDrift : BackCompatibilityConverter
{
    public override bool AppliesToVersion(int majorVer, int minorVer) => true;

    public override string? BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode? node = null)
    {
        if (defType == typeof(GeneDef))
            return defName switch
            {
                "XylHead_Trog" => "XylHead_Warped",
                "XylMineralizedSkin" => "XylRockskin",
                "XylPsyBeckon" => "XylPsycast_Beckon",
                _ => null,
            };

        if (defType == typeof(ThoughtDef) && defName == "XylSoreBreasts")
            return "XylFullMilk";
        if (defType == typeof(ThingDef) && defName == "XylFungusStill")
            return "Brewery";

        return null;
    }

    public override Type? GetBackCompatibleType(Type baseType, string providedClassName, XmlNode? node)
    {
        return providedClassName switch
        {
            "XylRacesCore.Hediff_BioRejection" => typeof(Hediff_BioRejection),
            "XylRacesCore.Hediff_DietDependency" => typeof(Hediff_DietDependency),
            "XylRacesCore.Hediff_ForceBehavior" => typeof(HediffWithComps),
            "XylRacesCore.Hediff_InnateWeapon" => typeof(HediffWithComps),
            "XylRacesCore.JobDriver_MilkHuman" => typeof(JobDriver_MilkHuman),
            "XylRacesCore.JobDriver_Soak" => typeof(JobDriver_Soak),
            "XylRacesCore.JobDriver_TakeShower" => typeof(JobDriver_TakeShower),
            "XylRacesCore.Need_Wetness" => typeof(Need_Wetness),
            "XylRacesCore.MentalState_HuntingVermin" => typeof(MentalState_HuntingVermin),
            "XylRacesCore.MentalState_SeeingRed" => typeof(MentalState_SeeingRed),
            "XylRacesCore.ScenPart_StartingSlaves" => typeof(ScenPart_StartingSlaves),
            "XylRacesCore.ScenPart_PsylinkLevels" => typeof(ScenPart_PsylinkLevels),
            "XylRacesCore.ScenPart_Psycast" => typeof(ScenPart_Psycast),
            _ when providedClassName.StartsWith("XylRacesCore.Genes.") => typeof(GeneWithComps),
            _ => null,
        };
    }

    public override void PostExposeData(object obj)
    {
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
            return;

        if (obj is GeneWithComps { comps: null } geneWithComps)
            geneWithComps.InitializeComps();
    }
}
