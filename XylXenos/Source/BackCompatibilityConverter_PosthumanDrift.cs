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

        if (defType == typeof(HeadTypeDef))
        {
            return defName switch
            {
                "Female_Osteoliths_Average_Normal" => "Female_AverageNormal",
                "Female_Osteoliths_Average_Wide" => "Female_AverageWide",
                "Female_Osteoliths_Narrow_Wide" => "Female_NarrowWide",
                "Male_Osteoliths_Average_Normal" => "Male_AverageNormal",
                "Male_Osteoliths_Average_Wide" => "Male_AverageWide",
                "Male_Osteoliths_Narrow_Wide" => "Male_NarrowWide",
                "Female_Scaleskin_Average_Normal" => "Female_AverageNormal",
                "Female_Scaleskin_Average_Pointy" => "Female_AveragePointy",
                "Female_Scaleskin_Narrow_Normal" => "Female_NarrowNormal",
                "Female_Scaleskin_Narrow_Pointy" => "Female_NarrowPointy",
                "Male_Scaleskin_Average_Normal" => "Male_AverageNormal",
                "Male_Scaleskin_Average_Pointy" => "Male_AveragePointy",
                "Male_Scaleskin_Narrow_Normal" => "Male_NarrowNormal",
                "Male_Scaleskin_Narrow_Pointy" => "Male_NarrowPointy",
                _ => null,
            };
        }

        if (defType == typeof(ThoughtDef) && defName == "XylSoreBreasts")
            return "XylFullMilk";

        if (defType == typeof(FactionDef) && defName == "XylSirenPlayerColony")
            return "PlayerColony";

        // Known issue: this converts all rage and backlash into rage with full remaining time
        if (defType == typeof(HediffDef) && defName == "XylFeralRageBacklash")
            return "XylFeralRage";

        if (defType == typeof(ThinkTreeDef) && defName == "XylSeeingRed")
            return "Humanlike";

        if (defType == typeof(ThingDef))
        {
            return defName switch
            {
                "XylFungusStill" => "Brewery",
                "Blueprint_XylFungusStill" => "Blueprint_Brewery",
                "Frame_XylFungusStill" => "Frame_Brewery",
                "Apparel_XylBlindfoldMask" => "Apparel_Blindfold",
                _ => null,
            };
        }

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
            // Known issue: ScenPart_PsylinkLevels and ScenPart_Psycast default to chance 0 after conversion
            "XylRacesCore.ScenPart_PsylinkLevels" => typeof(ScenPart_PsylinkLevels),
            "XylRacesCore.ScenPart_Psycast" => typeof(ScenPart_Psycast),
            "XylRacesCore.ScenPart_ConfigPage_ConfigureStartingPawns_Xenotypes_Ext" => typeof(ScenPart_ConfigPage_ConfigureStartingPawns_Xenotypes_Ext),
            _ when providedClassName.StartsWith("XylRacesCore.Genes.") => typeof(GeneWithComps),
            _ => null,
        };
    }

    public override void PostExposeData(object obj)
    {
    }
}
