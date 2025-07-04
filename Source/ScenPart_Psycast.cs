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
    public class ScenPart_Psycast : ScenPart
    {
        public int count = 1;
        public AbilityDef psycast;

        private string countBuf;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref count, "count");
            Scribe_Defs.Look(ref psycast, "psycast");
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 2f + 1f);

            var listing_Standard = new Listing_Standard();
            listing_Standard.Begin(scenPartRect.TopHalf());
            listing_Standard.ColumnWidth = scenPartRect.width;
            listing_Standard.TextFieldNumeric(ref count, ref countBuf, 1f);
            listing_Standard.End();

            if (!Widgets.ButtonText(scenPartRect.BottomHalf(), GetLabel(psycast)))
            {
                return;
            }
            var list = new List<FloatMenuOption>();
            foreach (AbilityDef item in PossiblePsycasts)
            {
                AbilityDef localDef = item;
                list.Add(new FloatMenuOption(GetLabel(localDef), delegate
                {
                    psycast = localDef;
                }));
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        private string GetLabel(AbilityDef abilityDef)
        {
            return "XylScenPartPsycastLabel".Translate(abilityDef.label.CapitalizeFirst(), abilityDef.level);
        }

        private List<AbilityDef> possiblePsycasts;
        private IEnumerable<AbilityDef> PossiblePsycasts => possiblePsycasts ??=
            DefDatabase<AbilityDef>.AllDefsListForReading.Where(abilityDef =>
                    abilityDef.verbProperties?.verbClass == typeof(Verb_CastPsycast))
                .OrderBy(abilityDef => abilityDef.level)
                .ThenBy(AbilityDef => AbilityDef.label).ToList();

        public override void Randomize()
        {
            psycast = PossiblePsycasts.RandomElement();
        }

        public override bool HasNullDefs()
        {
            if (base.HasNullDefs())
                return true;
            return psycast == null;
        }

        public override void GenerateIntoMap(Map map)
        {
            for (var i = 0; i < count; i++)
            {
                if (!map.mapPawns.FreeColonists.Where(CanLearnPsycast).TryRandomElement(out Pawn pawn))
                    return; 
                pawn.abilities.GainAbility(psycast);
            }
        }

        private bool CanLearnPsycast(Pawn pawn)
        {
            return CanLearnPsycast(pawn, psycast);
        }

        private bool CanLearnPsycast(Pawn pawn, AbilityDef abilityDef)
        {
            return pawn.GetPsylinkLevel() >= abilityDef.level && pawn.abilities.GetAbility(abilityDef) == null;
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == "PlayerStartsWith")
            {
                yield return "XylPsycast".TranslateSimple().CapitalizeFirst() + ": " + psycast.LabelCap + " x" + "1";
            }
        }
    }
}
