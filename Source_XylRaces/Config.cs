using System.Collections.Generic;
using System.Linq;
using Verse;

namespace XylXenos
{
    public class Config : Def
    {
        public enum Feature
        {
            FixLactationBugs,
        }

        public static Config Instance => instance ??= MakeConfig();


        private static Config instance;
        public List<JobDef> wetnessGivingJobs;

        public List<Feature> enabledFeatures;
        public List<string> ignoreGenesFromMods;

        private static Config MakeConfig()
        {
            Config config = new Config
            {
                wetnessGivingJobs = [],
                enabledFeatures = [],
                ignoreGenesFromMods = [],
            };

            foreach (var subConfig in DefDatabase<Config>.AllDefs)
            {
                config.wetnessGivingJobs.AddRange(subConfig.wetnessGivingJobs.EmptyIfNull());
                config.enabledFeatures.AddRange(subConfig.enabledFeatures.EmptyIfNull());
                config.ignoreGenesFromMods.AddRange(subConfig.ignoreGenesFromMods.EmptyIfNull());
            }

            return config;
        }

        public static bool FeatureEnabled(Feature feature)
        {
            bool result = Instance.enabledFeatures.EmptyIfNull().Contains(feature);
            Log.Message($"XylXenos feature check: {feature} = {result}");
            return result;
        }

        public static bool GeneOfTypeExists<T>()
        {
            bool result = DefDatabase<GeneDef>.AllDefs.Any(gene => typeof(T).IsAssignableFrom(gene.geneClass));
            Log.Message($"XylXenos feature check: {typeof(T)} = {result}");
            return result;
        }

        public static bool GeneWithModExtensionExists<T>()
        {
            bool result = DefDatabase<GeneDef>.AllDefs.Any(gene => gene.modExtensions.EmptyIfNull().OfType<T>().Any());
            Log.Message($"XylXenos feature check: {typeof(T)} = {result}");
            return result;
        }
    }
}
