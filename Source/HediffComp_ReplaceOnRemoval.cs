using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
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
