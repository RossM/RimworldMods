using System.Collections.Generic;
using System.Linq;
using Verse;

namespace XylRacesCore
{
    public class Config : Def
    {
        public List<JobDef> wetnessGivingJobs;

        public enum Feature
        {
            Gene_HostilityOverride,
        }

        public List<Feature> enabledFeatures;

        private static Config instance;

        public static Config Instance => instance ??= DefDatabase<Config>.AllDefs.FirstOrDefault() ?? new Config();

        public static bool FeatureEnabled(Feature feature)
        {
            return Instance.enabledFeatures?.Contains(feature) ?? false;
        }
    }
}
