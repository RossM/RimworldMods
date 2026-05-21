using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(IdeoUIUtility))]
    public static class Patch_IdeoUIUtility
    {
        [Feature(typeof(XenotypeDefExtension))]
        [HarmonyPostfix]
        [HarmonyPatch("GetMemeTip")]
        public static void GetMemeTip_Postfix(MemeDef meme, ref string __result)
        {
            StringBuilder sb = new StringBuilder(__result);

            List<XenotypeDef> agreeingXenotypes = [];
            List<XenotypeDef> disagreeingXenotypes = [];

            foreach (XenotypeDef def in DefDatabase<XenotypeDef>.AllDefs)
            {
                var modExt = def.GetModExtension<XenotypeDefExtension>();
                if (modExt != null)
                {
                    if (modExt.agreeingMemes?.Contains(meme) == true)
                        agreeingXenotypes.Add(def);
                    if (modExt.disagreeingMemes?.Contains(meme) == true)
                        disagreeingXenotypes.Add(def);
                }
            }

            if (!agreeingXenotypes.NullOrEmpty())
            {
                sb.AppendLine();
                sb.AppendInNewLine($"{"XylAgreeableXenotypes".Translate()}:".Colorize(ColoredText.TipSectionTitleColor));
                sb.AppendInNewLine(agreeingXenotypes.Select(def => def.label).ToLineList("  - ", capitalizeItems: true));
            }

            if (!disagreeingXenotypes.NullOrEmpty())
            {
                sb.AppendLine();
                sb.AppendInNewLine($"{"XylDisagreeableXenotypes".Translate()}:".Colorize(ColoredText.TipSectionTitleColor));
                sb.AppendInNewLine(disagreeingXenotypes.Select(def => def.label).ToLineList("  - ", capitalizeItems: true));
            }

            __result = sb.ToString();
        }
    }
}
