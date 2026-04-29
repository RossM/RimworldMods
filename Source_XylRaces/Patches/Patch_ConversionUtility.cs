using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using TranspilerUtil;
using UnityEngine;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(ConversionUtility))]
    public static class Patch_ConversionUtility
    {
        private static readonly InstructionMatcher Fixup_ConversionPowerFactor_MemesVsTraits = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.InsertBefore,
                    Pattern =
                    [
                        new CodeInstruction(OpCodes.Ldc_R4, -0.4f),
                        CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Max), [typeof(float), typeof(float)]),
                    ],
                    Output =
                    [
                        // + OffsetFromXenotype(initiator, false, sb)
                        CodeInstruction.LoadArgument(0),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        CodeInstruction.LoadArgument(2),
                        CodeInstruction.Call(() => OffsetFromXenotype),
                        new CodeInstruction(OpCodes.Add),
                        // + OffsetFromXenotype(recipient, true, sb)
                        CodeInstruction.LoadArgument(1),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        CodeInstruction.LoadArgument(2),
                        CodeInstruction.Call(() => OffsetFromXenotype),
                        new CodeInstruction(OpCodes.Add),
                    ]
                }
            }
        };

        [Feature(nameof(XenotypeDefExtension)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(nameof(ConversionUtility.ConversionPowerFactor_MemesVsTraits))]
        public static IEnumerable<CodeInstruction> ConversionPowerFactor_MemesVsTraits_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_ConversionPowerFactor_MemesVsTraits.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        public static float OffsetFromXenotype(Pawn pawn, bool invert, StringBuilder sb)
        {
            using (new ProfileBlock())
            {
                float result = 0;
                string text = string.Empty;
                XenotypeDef pawnXenotype = pawn.genes?.Xenotype;
                if (pawnXenotype == null)
                    return 0;

                var agreeingMemes = pawnXenotype.GetModExtension<XenotypeDefExtension>()?.agreeingMemes;
                if (agreeingMemes != null)
                {
                    foreach (MemeDef meme in pawn.Ideo.memes)
                    {
                        if (agreeingMemes.Contains(meme))
                        {
                            float offset = invert ? -0.2f : 0.2f;
                            result += offset;
                            text += MemeAndXenotypeDesc(meme, pawnXenotype, offset);
                        }
                    }
                }

                var disagreeingMemes = pawnXenotype.GetModExtension<XenotypeDefExtension>()?.disagreeingMemes;
                if (disagreeingMemes != null)
                {
                    foreach (MemeDef meme in pawn.Ideo.memes)
                    {
                        if (disagreeingMemes.Contains(meme))
                        {
                            float offset = invert ? 0.2f : -0.2f;
                            result += offset;
                            text += MemeAndXenotypeDesc(meme, pawnXenotype, offset);
                        }
                    }
                }

                if (sb != null && !text.NullOrEmpty())
                {
                    sb.AppendInNewLine(" -  " + "AbilityIdeoConvertBreakdownPawnIdeo".Translate(pawn.Named("PAWN")) +
                                       ": " + text);
                }

                return result;
            }

            string MemeAndXenotypeDesc(MemeDef meme, XenotypeDef xenotype, float offset)
            {
                if (sb == null)
                {
                    return string.Empty;
                }
                // Adding 1 to the offset and reporting it as a percentage is complete nonsense and gives the impression
                // that these are factors being multiplied together rather than added. However, it's complete nonsense
                // that matches what the base game does for traits, so I am holding my nose and matching it.
                return "\n   -  " + "XylAbilityIdeoConvertBreakdownMemeVsXenotype".Translate(meme.label.Named("MEME"), xenotype.label.Named("XENOTYPE")).CapitalizeFirst() + ": " + (1f + offset).ToStringPercent();
            }
        }
    }
}
