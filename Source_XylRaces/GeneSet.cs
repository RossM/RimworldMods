using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using XylXenos.Genes;

namespace XylXenos
{
    public class GeneSet(Pawn pawn) : INotificationListener
    {
        public static readonly PawnTracker<GeneSet> Tracker = new(Make);
        public Pawn pawn = pawn;

        public float bodySizeFactor = 1f;
        public float healthScaleFactor = 1f;
        public float slaveRebellionMtbFactor = 1f;
        public float slaveRebellionThresholdDays = float.MaxValue;
        public float manhunterOnDamageChanceFactor = 1f;
        public float manhunterOnTameFailChanceFactor = 1f;
        public List<JoyGiverFactor> joyGiverChanceFactors = [];
        public List<BuildableDef> addDesignators = [];
        public List<RenderNodeModifier> renderNodeModifiers = [];
        public List<FactionDef> disableHostilityFromFactions = [];
        public List<GeneIngestionThoughtOverride> ingestionThoughtOverrides = [];

        private static GeneSet Make(Pawn pawn)
        {
            var geneSet = new GeneSet(pawn);
            geneSet.RegisterWith(NotificationManager.Instance);
            geneSet.Update();
            return geneSet;
        }

        public void Update()
        {
            bodySizeFactor = 1f;
            healthScaleFactor = 1f;
            slaveRebellionMtbFactor = 1f;
            slaveRebellionThresholdDays = float.MaxValue;
            manhunterOnDamageChanceFactor = 1f;
            manhunterOnTameFailChanceFactor = 1f;
            joyGiverChanceFactors.Clear();
            addDesignators.Clear();
            renderNodeModifiers.Clear();
            disableHostilityFromFactions.Clear();
            ingestionThoughtOverrides.Clear();

            foreach (var def in pawn.ActiveExtendedGeneDefs())
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
                if (!def.disableHostilityFromFactions.NullOrEmpty())
                    disableHostilityFromFactions.AddRange(def.disableHostilityFromFactions);
                if (!def.ingestionThoughtOverrides.NullOrEmpty())
                    ingestionThoughtOverrides.AddRange(def.ingestionThoughtOverrides);
            }
        }

        public void Notify_PostGenesChanged()
        {
            Update();
        }

        public void Notify_PostLoadedGame()
        {
            Update();
        }

        public void RegisterWith(NotificationManager manager)
        {
            manager.Register(NotificationEvent.PostGenesChanged, pawn, Notify_PostGenesChanged);
            manager.Register(NotificationEvent.PostLoadedGame, pawn, Notify_PostLoadedGame);
        }
    }
}
