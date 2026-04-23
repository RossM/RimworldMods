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
            FixLactationBugs,
        }

        public List<Feature> enabledFeatures;

        private static Config instance;

        public static Config Instance => instance ??= MakeConfig();

        private static Config MakeConfig()
        {
            Config config = new Config
            {
                wetnessGivingJobs = [],
                enabledFeatures = []
            };

            foreach (var subConfig in DefDatabase<Config>.AllDefs)
            {
                config.wetnessGivingJobs.AddRange(subConfig.wetnessGivingJobs.EmptyIfNull());
                config.enabledFeatures.AddRange(subConfig.enabledFeatures.EmptyIfNull());
            }

            return config;
        }

        public static bool FeatureEnabled(Feature feature)
        {
            return Instance.enabledFeatures.EmptyIfNull().Contains(feature);
        }

        public static bool GeneOfTypeExists<T>()
        {
            bool result = DefDatabase<GeneDef>.AllDefs.Any(gene => typeof(T).IsAssignableFrom(gene.geneClass));
            Log.Message($"XylRacesCore feature check: {typeof(T)} = {result}");
            return result;
        }

        public static bool GeneWithModExtensionExists<T>()
        {
            bool result = DefDatabase<GeneDef>.AllDefs.Any(gene => gene.modExtensions.EmptyIfNull().OfType<T>().Any());
            Log.Message($"XylRacesCore feature check: {typeof(T)} = {result}");
            return result;
        }
    }
}
