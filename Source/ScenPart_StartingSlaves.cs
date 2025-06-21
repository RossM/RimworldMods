using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    internal class ScenPart_StartingSlaves : ScenPart
    {
        public int count;

        private string countBuf;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref count, "count");
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 1f + 1f);
            scenPartRect.height = ScenPart.RowHeight;
            Widgets.TextFieldNumeric(scenPartRect, ref count, ref countBuf, 1);
        }

        public override string Summary(Scenario scen)
        {
            return ScenSummaryList.SummaryWithList(scen, "PlayerStartsWith", ScenPart_StartingThing_Defined.PlayerStartWithIntro);
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == "PlayerStartsWith")
            {
                yield return "slave".TranslateSimple().CapitalizeFirst() + " x" + count;
            }
        }

        public override void Randomize()
        {
            count = 1;
        }

        public override IEnumerable<Thing> PlayerStartingThings()
        {
            for (int i = 0; i < count; i++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.Slave, null);
                Pawn slave = PawnGenerator.GeneratePawn(request);
                slave.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
                yield return slave;
            }
        }
        // GenGuest.TryEnslavePrisoner
    }
}
