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

        using (new ProfileBlock("Harmony patching"))
            harmony.PatchCategory("PostLoadDefs");

        // TODO Split infix patching into early and late
        using (new ProfileBlock("Infix patching"))
            InfixPatcher.PatchInfix(harmony, Assembly.GetExecutingAssembly());
    }
}

[UsedFromReflection]
[StaticConstructorOnStartup]
public class Main : Mod
{
    public Main(ModContentPack content) : base(content)
    {
        using (new ProfileBlock("Load settings"))
            Settings.instance = GetSettings<Settings>();

        using (new ProfileBlock("Coding style checks"))
            CodingStyleChecks();

        var harmony = new Harmony("net.pardeike.rimworld.lib.harmony");

        using (new ProfileBlock("Harmony patching"))
        {
            harmony.PatchCategory("PreLoadDefs");
            harmony.PatchCategory(null);
        }

        using (new ProfileBlock("Register XML loaders"))
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
            if (xmlRoot.HasChildNodes)
                data.degree = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
        }
    }

    private static void CodingStyleChecks()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (TypeInfo type in assembly.DefinedTypes)
        {
            if (!Attribute.IsDefined(type, typeof(HarmonyPatch)))
                continue;

            foreach (MethodInfo method in type.DeclaredMethods)
            {
                var hasFeature = method.HasAttribute<FeatureAttribute>();
                var hasPrefix = method.HasAttribute<HarmonyPrefix>();
                var hasPostfix = method.HasAttribute<HarmonyPostfix>();
                var hasTranspiler = method.HasAttribute<HarmonyTranspiler>();
                var hasInfixPatch = method.HasAttribute<InfixPatchAttribute>();
                var hasInfixPrefix = method.HasAttribute<InfixPrefixAttribute>();
                var hasInfixPostfix = method.HasAttribute<InfixPostfixAttribute>();

                if ((hasPrefix || hasPostfix || hasTranspiler || hasInfixPatch) && !hasFeature)
                    Log.Warning($"{type.Name}::{method.Name} is missing a [Feature] attribute");
                if (!(hasPrefix || hasPostfix || hasTranspiler || hasInfixPatch) && hasFeature)
                    Log.Warning($"{type.Name}::{method.Name} has [Feature] but no Harmony attribute");

                if (hasInfixPatch != (hasInfixPrefix || hasInfixPostfix))
                    Log.Warning(
                        $"{type.Name}::{method.Name} has should have both [InfixPatch] and one of [InfixPrefix] or [InfixPostfix]");

                if ((hasPrefix || hasInfixPrefix) && !(method.Name == "Prefix" || method.Name.EndsWith("_Prefix")))
                    Log.Warning($"{type.Name}::{method.Name} should be named with _Prefix");
                if ((hasPostfix || hasInfixPostfix) && !(method.Name == "Postfix" || method.Name.EndsWith("_Postfix")))
                    Log.Warning($"{type.Name}::{method.Name} should be named with _Postfix");
                if (hasTranspiler && !(method.Name == "Transpiler" || method.Name.EndsWith("_Transpiler")))
                    Log.Warning($"{type.Name}::{method.Name} should be named with _Transpiler");

                var parameters = method.GetParameters();
                ParameterInfo resultParameter = parameters.SingleOrDefault(p => p.Name == "__result");
                if (hasPrefix || hasInfixPrefix)
                {
                    if (resultParameter?.IsOut == false)
                        Log.Warning($"{type.Name}::{method.Name} should use 'out' for __result");
                    if (method.ReturnType.IsVoid() && resultParameter != null)
                        Log.Warning($"{type.Name}::{method.Name} returns void but uses __result");
                }

                if (hasPostfix || hasInfixPostfix)
                {
                    if (resultParameter is { ParameterType.IsByRef: false })
                        Log.Warning($"{type.Name}::{method.Name} has a non-ref __result");
                }
            }
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
