using System.Xml;

namespace XylXenos;

[StaticConstructorOnStartup]
public static class PatchLate
{
    static PatchLate()
    {
        using (new ProfileBlock("XylXenos Disharmony patching"))
        {
            Patcher.PatchCategory(typeof(PatchLate).Assembly, "PostLoadDefs");
        }
    }
}

[UsedFromReflection]
[StaticConstructorOnStartup]
public class Main : Mod
{
    public Main(ModContentPack content) : base(content)
    {
        RegisterForBackCompatibility();

        using (new ProfileBlock("XylXenos Load settings"))
        {
            Settings.instance = GetSettings<Settings>();
        }

        using (new ProfileBlock("XylXenos Disharmony patching"))
        {
            var assembly = typeof(Main).Assembly;
            Patcher.PatchCategory(assembly, "PreLoadDefs");
            Patcher.PatchCategory(assembly, null);

            if (ModLister.GetActiveModWithIdentifier(ModIdOf.DubsBadHygiene) is not null)
                Patcher.PatchCategory(assembly, ModIdOf.DubsBadHygiene);
        }

        using (new ProfileBlock("XylXenos Register XML loaders"))
        {
            RegisterXmlLoaders();
        }
    }

    private static void RegisterForBackCompatibility()
    {
        BackCompatibility.conversionChain.Add(new BackCompatibilityConverter_PosthumanDrift());
        BackCompatibility.RemovedDefs.Add(new("Osteoliths", typeof(FurDef)));
        BackCompatibility.RemovedDefs.Add(new("Scaleskin", typeof(FurDef)));
    }

    // This a stupid trick to add a custom XML parser to a type that should have one but doesn't.
    private static void RegisterXmlLoaders()
    {
        DebugAssert.NotNull(XmlToObjectUtils.customDataLoadMethodCache);

        XmlToObjectUtils.customDataLoadMethodCache[typeof(GeneticTraitData)]
            = ((Action<GeneticTraitData, XmlNode>)GeneticTraitData_LoadDataFromXmlCustom).Method;
    }

    public static void GeneticTraitData_LoadDataFromXmlCustom(GeneticTraitData data, XmlNode xmlRoot)
    {
        if (xmlRoot.Name == "li")
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(data, "def",
                xmlRoot.ChildNodes.OfType<XmlNode>().Single(node => node.Name == "def").InnerText);
            if (xmlRoot.ChildNodes.OfType<XmlNode>().SingleOrDefault(node => node.Name == "degree") is { } degreeNode)
                data.degree = ParseHelper.FromString<int>(degreeNode.InnerText);
        }
        else
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(data, "def", xmlRoot.Name);
            if (xmlRoot.FirstChild?.Value is not null)
                data.degree = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
        }
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.instance.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return Content.Name;
    }
}
