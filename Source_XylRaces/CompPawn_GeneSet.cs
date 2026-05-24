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

        public void RegisterWith(NotificationManager manager)
        {
            Debug.Log($"CompPawn_GeneSet RegisterWith for {parent}");
            manager.Register(NotificationEvent.PostGenesChanged, parent, Notify_GenesChanged);
        }

        public void Update()
        {
            bodySizeFactor = 1.0f;
            healthScaleFactor = 1.0f;
            joyGiverChanceFactors.Clear();
            addDesignators.Clear();

            foreach (var def in ((Pawn)parent).ExtendedGeneDefs())
            {
                bodySizeFactor *= def.bodySizeFactor;
                healthScaleFactor *= def.healthScaleFactor;

                if (!def.joyGiverChanceFactors.NullOrEmpty())
                    joyGiverChanceFactors.AddRange(def.joyGiverChanceFactors);
                if (!def.addDesignators.NullOrEmpty())
                    addDesignators.AddRange(def.addDesignators);
            }
        }

        public override void PostPostMake()
        {
            Debug.Log($"CompPawn_GeneSet PostPostMake for {parent}");
            Update();
        }

        public override void PostMapInit()
        {
            Debug.Log($"CompPawn_GeneSet PostMapInit for {parent}");
            Update();
        }

        private void Notify_GenesChanged()
        {
            Update();
        }
    }
}
