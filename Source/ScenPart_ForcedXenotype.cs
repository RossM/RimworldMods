using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    public class ScenPart_ForcedXenotype : ScenPart_PawnModifier
    {
        protected XenotypeDef xenotype;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref xenotype, "xenotype");
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 3f);
            if (Widgets.ButtonText(scenPartRect.TopPart(0.333f), xenotype.LabelCap))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (XenotypeDef item in DefDatabase<XenotypeDef>.AllDefs.OrderBy((XenotypeDef xd) => xd.label))
                {
                    XenotypeDef localDef = item;
                    list.Add(new FloatMenuOption(localDef.LabelCap, delegate
                    {
                        xenotype = localDef;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
            DoPawnModifierEditInterface(scenPartRect.BottomPart(0.666f));
        }

        public override string Summary(Scenario scen)
        {
            return string.Format("{0} have a {1} chance to start with xenotype: {2}", context.ToStringHuman(), chance.ToStringPercent(), xenotype.LabelCap).CapitalizeFirst();
        }

        public override void Randomize()
        {
            base.Randomize();
            xenotype = DefDatabase<XenotypeDef>.GetRandom();
        }

        public override bool CanCoexistWith(ScenPart other)
        {
            if (other is ScenPart_ForcedXenotype scenPart_forcedXenotype && xenotype == scenPart_forcedXenotype.xenotype && context.OverlapsWith(scenPart_forcedXenotype.context))
            {
                return false;
            }
            return true;
        }

        public override bool HasNullDefs()
        {
            if (base.HasNullDefs()) 
                return true;
            return xenotype == null;
        }

        public void ModifyXenotype(Faction faction, PawnGenerationContext context, ref XenotypeDef result)
        {
            if (!this.context.Includes(context))
                return;
            if (hideOffMap && context != PawnGenerationContext.PlayerStarter)
                return;
            if (!Rand.Chance(chance))
                return;

            result = xenotype;
        }
    }
}
