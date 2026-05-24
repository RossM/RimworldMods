using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using XylXenos.Genes;

namespace XylXenos
{
    [UsedFromXml]
    public class CompProperties_PawnGeneSet : CompProperties
    {
        public CompProperties_PawnGeneSet()
        {
            compClass = typeof(CompPawn_GeneSet);
        }
    }

    public class CompPawn_GeneSet : ThingComp, INotificationListener
    {
        [Unsaved] public float bodySizeFactor = 1.0f;
        [Unsaved] public float healthScaleFactor = 1.0f;
        [Unsaved] public List<JoyGiverFactor> joyGiverChanceFactors = [];
        [Unsaved] public List<BuildableDef> addDesignators = [];
        [Unsaved] public List<RenderNodeModifier> renderNodeModifiers = [];

        public void RegisterWith(NotificationManager manager)
        {
            manager.Register(NotificationEvent.PostGenesChanged, parent, Notify_GenesChanged);
        }

        public void Update()
        {
            bodySizeFactor = 1.0f;
            healthScaleFactor = 1.0f;
            joyGiverChanceFactors.Clear();
            addDesignators.Clear();
            renderNodeModifiers.Clear();

            foreach (var def in ((Pawn)parent).ExtendedGeneDefs())
            {
                bodySizeFactor *= def.bodySizeFactor;
                healthScaleFactor *= def.healthScaleFactor;

                if (!def.joyGiverChanceFactors.NullOrEmpty())
                    joyGiverChanceFactors.AddRange(def.joyGiverChanceFactors);
                if (!def.addDesignators.NullOrEmpty())
                    addDesignators.AddRange(def.addDesignators);
                if (!def.renderNodeModifiers.NullOrEmpty())
                    renderNodeModifiers.AddRange(def.renderNodeModifiers);
            }
        }

        public override void PostPostMake()
        {
            Update();
        }

        public override void PostMapInit()
        {
            Update();
        }

        private void Notify_GenesChanged()
        {
            Update();
        }
    }
}
