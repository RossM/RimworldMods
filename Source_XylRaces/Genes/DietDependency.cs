using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylXenos.Genes
{
    public class DietDependencyInfo
    {
        public FoodKind foodKind = FoodKind.Any;
        public bool rawOnly = false;
        public float severityReductionPerNutrition = 1f;
        [MustTranslate] public string foodLabel;
    }
}
