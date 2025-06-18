using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    internal class ModExtension_GeneDef_DietDependency : DefModExtension
    {
        public HediffDef hediffDef;
        public FoodKind foodKind = FoodKind.Any;
        public bool rawOnly = false;
        public float severityReductionPerNutrition = 1f;
    }
}
