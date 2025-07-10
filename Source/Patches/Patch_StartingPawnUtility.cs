using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(StartingPawnUtility))]
    public class Patch_StartingPawnUtility
    {
        private static readonly InstructionMatcher Fixup_GetPsycastLevel = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 1,
                    Mode = InstructionMatcher.OutputMode.InsertAfter,
                    Pattern =
                    [
                        CodeInstruction.Call(typeof(Rand), "get_" + nameof(Rand.Value)),
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
                }
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch("GeneratePossessions")]
        public static IEnumerable<CodeInstruction> GeneratePossessions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GetPsycastLevel.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        public static void GetExtraStartingItems(Pawn pawn, List<ThingDefCount> items)
        {
            using (new ProfileBlock())
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
        }
    }
}
