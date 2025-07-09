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
    public class ScenPart_StartingSlaves : ScenPart_PawnModifier
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
            Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 1f + 1f);

            scenPartRect.height = RowHeight;
            Widgets.TextFieldNumeric(scenPartRect, ref count, ref countBuf, 1);

        }

        public override string Summary(Scenario scen)
        {
            return "XylStartingSlaves".Translate(count);
        }

        public override void Randomize()
        {
            count = 1;
        }

        public override void GenerateIntoMap(Map map)
        {
            List<Pawn> pawns = map.mapPawns.FreeColonists;
            for (int i = Math.Max(0, pawns.Count - count); i < pawns.Count; i++)
            {
                var pawn = pawns[i];
                pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
            }
        }
    }
}
