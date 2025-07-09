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
            base.GenerateStartingPawns();

            if (Find.GameInitData.startingAndOptionalPawns.Count >= pawnChoiceCount)
                return;

            // Make sure that the extra reserve pawns are an appropriate xenotype
            int index = Find.GameInitData.startingAndOptionalPawns.Count;
            var request = StartingPawnUtility.GetGenerationRequest(index);
            request.ForcedXenotype = xenotypeCounts[xenotypeCounts.Count - 1].xenotype;
            StartingPawnUtility.SetGenerationRequest(index, request);

            while (Find.GameInitData.startingAndOptionalPawns.Count < pawnChoiceCount)
            {
                StartingPawnUtility.AddNewPawn(index);
            }
        }
    }
}
