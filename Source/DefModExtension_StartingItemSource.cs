using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class DefModExtension_StartingItemSource : DefModExtension, IStartingItemSource
    {
        public ThingDef item;
        public float chance = 1.0f;
        public IntRange count = IntRange.One;

        public ThingDefCount? GetStartingItem()
        {
            if (item == null)
                return null;
            if (!Rand.Chance(chance)) 
                return null;
            return new(item, Mathf.Clamp(count.RandomInRange, 1, item.stackLimit));
        }
    }
}
