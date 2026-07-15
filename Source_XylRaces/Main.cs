using System.Diagnostics;
using System.Reflection;
using System.Xml;

namespace XylXenos;

[StaticConstructorOnStartup]
public static class PatchLate
{
    static PatchLate()
    {
        var harmony = new Harmony("net.pardeike.rimworld.lib.harmony");

        using (new ProfileBlock("XylXenos Harmony patching"))
            harmony.PatchCategory("PostLoadDefs");

        // TODO Split infix patching into early and late
        using (new ProfileBlock("XylXenos Infix patching"))
            Autopatcher.PatchAll(Assembly.GetExecutingAssembly());
    }
}

[UsedFromReflection]
[StaticConstructorOnStartup]
public class Main : Mod
{
    public Main(ModContentPack content) : base(content)
    {
        using (new ProfileBlock("XylXenos Load settings"))
            Settings.instance = GetSettings<Settings>();

        using (new ProfileBlock("XylXenos CheckCodingStyle"))
            Analyzer.CheckCodingStyle(typeof(Main).Assembly);

        var harmony = new Harmony("Xylthixlm.Races.Core");

        using (new ProfileBlock("XylXenos Harmony patching"))
        {
            harmony.PatchCategory("PreLoadDefs");
            harmony.PatchAllUncategorized();
        }

        using (new ProfileBlock("XylXenos Register XML loaders"))
            RegisterXmlLoaders();
    }

    [DebugAction(allowedGameStates = AllowedGameStates.Entry)]
    public static void DebuggerBreak()
    {
        Debugger.Break();
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
