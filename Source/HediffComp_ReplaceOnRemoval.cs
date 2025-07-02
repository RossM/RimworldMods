using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    // ReSharper disable once UnusedMember.Global
    public class HediffComp_ReplaceOnRemoval : HediffComp_ReplaceHediff
    {
        [Unsaved(false)] 
        private bool hasTriggered = false;

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (hasTriggered) 
                return;
            hasTriggered = true;
            Trigger();
        }
    }
}
