using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class ScenPart_ConfigPage_ConfigureStartingPawns_Xenotypes_Ext : ScenPart_ConfigPage_ConfigureStartingPawns_Xenotypes
    {
        protected override void GenerateStartingPawns()
        {
            foreach (var xenotypeCount in xenotypeCounts) 
                xenotypeCount.xenotype ??= XenotypeDefOf.Baseliner;

            base.GenerateStartingPawns();

            while (Find.GameInitData.startingAndOptionalPawns.Count < pawnChoiceCount)
            {
                // Make sure that the extra reserve pawns are appropriate xenotypes
                int index = Find.GameInitData.startingAndOptionalPawns.Count;
                var request = StartingPawnUtility.GetGenerationRequest(index);
                request.ForcedXenotype = xenotypeCounts.RandomElementByWeight(x => x.count).xenotype;
                StartingPawnUtility.SetGenerationRequest(index, request);

                StartingPawnUtility.AddNewPawn(index);
            }
        }
    }
}
