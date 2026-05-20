using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(StartingPawnUtility))]
    public class Patch_StartingPawnUtility
    {
        private static readonly InstructionMatcher Fixup_GeneratePossessions = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 1,
                    Mode = InstructionMatcher.OutputMode.InsertAfter,
                    Pattern =
                    [
                        CodeInstruction.Call(typeof(Rand), $"get_{nameof(Rand.Value)}"),
                    ],
                    Output =
                    [
                        // Load pawn
                        CodeInstruction.LoadArgument(0),
                        // Load StartingPawnUtility.StartingPossessions
                        CodeInstruction.Call(typeof(StartingPawnUtility), "get_StartingPossessions"),
                        // Load pawn
                        CodeInstruction.LoadArgument(0),
                        // Get StartingPawnUtility.StartingPossessions[pawn]
                        CodeInstruction.Call(typeof(Dictionary<Pawn, List<ThingDefCount>>), "get_Item"),
                        // Call GetExtraStartingItems
                        CodeInstruction.Call(() => GetExtraStartingItems),
                    ]
                },

                InstructionMatcher.MakeRedirectRule(
                    AccessTools.Method(typeof(List<ThingDefCount>), "Add"),
                    List_Add_Wrapper
                )
            }
        };

        [Feature(typeof(IStartingItemSource))]
        [HarmonyTranspiler]
        [HarmonyPatch("GeneratePossessions")]
        public static IEnumerable<CodeInstruction> GeneratePossessions_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GeneratePossessions.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static void GetExtraStartingItems(Pawn pawn, List<ThingDefCount> items)
        {
            foreach (var startingItemSource in pawn.EverythingOfType<IStartingItemSource>())
            {
                if (startingItemSource.GetStartingItem() is not { } item)
                    continue;

                items.Add(item);

                if (items.Count >= 2)
                    return;
            }
        }

        public static void List_Add_Wrapper(List<ThingDefCount> __instance, ThingDefCount item, Pawn pawn)
        {
            var chemical = item.ThingDef.GetCompProperties<CompProperties_Drug>()?.chemical;
            if (chemical != null && !pawn.ChemicalIsAllowedByGenes(chemical))
                return;

            __instance.Add(item);
        }
    }
}
