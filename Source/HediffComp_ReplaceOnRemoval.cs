using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class HediffComp_ReplaceOnRemoval : HediffComp_ReplaceHediff
    {
        [Unsaved()] 
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
