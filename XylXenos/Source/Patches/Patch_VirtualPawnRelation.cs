using System;
using System.Collections.Generic;
using System.Text;
using Waylith.Injection;

namespace XylXenos.Patches
{
    internal static class VirtualPawnRelationExtensions
    {
        extension(VirtualPawnRelation _)
        {
            [InjectedField]
            public extern ref Gender OriginalGender { get; }
        }
    }

    [Patch(typeof(VirtualPawnRelation))]
    public static class Patch_VirtualPawnRelation
    {
        [Feature(typeof(GeneCompProperties_GenderRatio))]
        [Postfix]
        [Target(nameof(VirtualPawnRelation.ExposeData))]
        public static void ExposeData_Postfix(VirtualPawnRelation __instance)
        {
            Scribe_Values.Look(ref __instance.OriginalGender, "XylOriginalGender");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && __instance is { OriginalGender: Gender.None, record: not null })
                __instance.OriginalGender = __instance.record.Gender;
        }
    }
}
