﻿using Verse;

namespace XylRacesCore
{ 
    public interface IGene_HediffSource
    {
        bool CausesHediff(HediffDef hediffDef);
    }

    public class Hediff_Genetic : HediffWithComps
    {
        [Unsaved()]
        private Gene cachedGene;

        public override bool ShouldRemove => Gene is not { Active: true };

        public Gene Gene => cachedGene ??=
            pawn.genes?.GenesListForReading.FirstOrDefault(gene =>
                gene is IGene_HediffSource hediffSource && hediffSource.CausesHediff(def));

        public override float Severity
        {
            get => Gene is not { Active: true } ? def.initialSeverity : base.Severity;
            set => base.Severity = value;
        }
    }
}
