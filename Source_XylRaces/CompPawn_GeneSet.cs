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
        [Unsaved] public float bodySizeFactor = 1f;
        [Unsaved] public float healthScaleFactor = 1f;
        [Unsaved] public float slaveRebellionMtbFactor = 1f;
        [Unsaved] public float slaveRebellionThresholdDays = float.MaxValue;
        [Unsaved] public float manhunterOnDamageChanceFactor = 1f;
        [Unsaved] public float manhunterOnTameFailChanceFactor = 1f;
        [Unsaved] public List<JoyGiverFactor> joyGiverChanceFactors = [];
        [Unsaved] public List<BuildableDef> addDesignators = [];
        [Unsaved] public List<RenderNodeModifier> renderNodeModifiers = [];

        public void RegisterWith(NotificationManager manager)
        {
            manager.Register(NotificationEvent.PostGenesChanged, parent, Notify_GenesChanged);
        }

        public void Update()
        {
            bodySizeFactor = 1f;
            healthScaleFactor = 1f;
            slaveRebellionMtbFactor = float.MaxValue;
            slaveRebellionThresholdDays = -1f;
            manhunterOnDamageChanceFactor = 1f;
            manhunterOnTameFailChanceFactor = 1f;
            joyGiverChanceFactors.Clear();
            addDesignators.Clear();
            renderNodeModifiers.Clear();

            foreach (var def in ((Pawn)parent).ActiveExtendedGeneDefs())
            {
                bodySizeFactor *= def.bodySizeFactor;
                healthScaleFactor *= def.healthScaleFactor;
                slaveRebellionMtbFactor *= def.slaveRebellionMtbFactor;
                slaveRebellionThresholdDays = Mathf.Min(slaveRebellionThresholdDays, def.slaveRebellionThresholdDays);
                manhunterOnDamageChanceFactor *= def.manhunterOnDamageChanceFactor;
                manhunterOnTameFailChanceFactor *= def.manhunterOnTameFailChanceFactor;

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
