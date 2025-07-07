using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class ScenPart_PsylinkLevels : ScenPart
    {
        public int count = 1;
        public bool givePsycasts = true;

        private string countBuf;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref count, "count");
            Scribe_Values.Look(ref givePsycasts, "givePsycasts");
        }

        public override void GenerateIntoMap(Map map)
        {
            for (var i = 0; i < count; ++i)
            {
                Pawn pawn = map.mapPawns.FreeColonists.RandomElementByWeight(PawnWeight);
                if (pawn != null)
                {
                    if (givePsycasts)
                        pawn.ChangePsylinkLevel(1, false);
                    else
                        ChangePsylinkLevelWithoutAbility(pawn, 1, false);
                }
            }
        }

        public static void ChangePsylinkLevelWithoutAbility(Pawn pawn, int levelOffset, bool sendLetter = true)
        {
            Hediff_Psylink mainPsylinkSource = pawn.GetMainPsylinkSource();
            if (mainPsylinkSource == null)
            {
                mainPsylinkSource = (Hediff_Psylink)HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn);
                try
                {
                    var oldAbilities = new HashSet<Ability>(pawn.abilities.AllAbilitiesForReading);
                    mainPsylinkSource.suppressPostAddLetter = !sendLetter;
                    pawn.health.AddHediff(mainPsylinkSource, pawn.health.hediffSet.GetBrain());
                    foreach (var newAbility in pawn.abilities.AllAbilitiesForReading.Where(a => !oldAbilities.Contains(a)))
                        pawn.abilities.RemoveAbility(newAbility.def);
                    levelOffset -= 1;
                }
                finally
                {
                    mainPsylinkSource.suppressPostAddLetter = false;
                }
            }
            if (levelOffset > 0)
            {
                float num = Math.Min(levelOffset, mainPsylinkSource.def.maxSeverity - (float)mainPsylinkSource.level);
                for (var i = 0; (float)i < num; i++)
                {
                    pawn.psychicEntropy?.Notify_GainedPsylink();
                }
            }

            mainPsylinkSource.level = (int)Mathf.Clamp(mainPsylinkSource.level + levelOffset, mainPsylinkSource.def.minSeverity, mainPsylinkSource.def.maxSeverity);
        }

        private static float PawnWeight(Pawn Pawn)
        {
            return (1 + Pawn.GetPsylinkLevel()) * Pawn.GetStatValue(StatDefOf.PsychicSensitivity);
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 2f + 1f);
            scenPartRect.height = RowHeight;
            Widgets.TextFieldNumeric(scenPartRect, ref count, ref countBuf, 1, 10);
            scenPartRect.y += RowHeight;
            Widgets.CheckboxLabeled(scenPartRect, "Give psycasts", ref givePsycasts);
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == "PlayerStartsWith")
            {
                yield return StatDefOf.Ability_RequiredPsylink.LabelCap + " x" + count;
            }
        }
    }
}
