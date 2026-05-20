using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch]
    public static class PatchPartEfficiencyOffset
    {
        public static AccessTools.FieldRef<object, Hediff> hediffGetter;

        private static readonly InstructionMatcher.Rule Rule_HediffStage_partEfficiencyOffset = InstructionMatcher.MakeRedirectRule(
            AccessTools.Field(typeof(HediffStage), nameof(HediffStage.partEfficiencyOffset)),
            HediffStage_partEfficiencyOffset_Wrapper);

        [UsedImplicitly]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type iteratorType = AccessTools.InnerTypes(typeof(HediffStatsUtility))
                .First(type => type.Name.Contains("<SpecialDisplayStats>"));
            hediffGetter = AccessTools.FieldRefAccess<Hediff>(iteratorType, "<>3__instance");
            yield return AccessTools.Method(iteratorType, "MoveNext");
        }


        [Feature(typeof(HediffWithCompsExt))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    Rule_HediffStage_partEfficiencyOffset
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static float HediffStage_partEfficiencyOffset_Wrapper(HediffStage __instance, object __caller)
        {
            var hediff = hediffGetter(__caller);
            if (hediff is HediffWithCompsExt ext)
                return ext.PartEfficiencyOffset;

            return __instance.partEfficiencyOffset;
        }
    }
}
