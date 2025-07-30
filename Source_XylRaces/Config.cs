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
        }

        public List<Feature> enabledFeatures;

        private static Config instance;

        public static Config Instance => instance ??= DefDatabase<Config>.AllDefs.FirstOrDefault() ?? new Config();

        public static bool FeatureEnabled(Feature feature)
        {
            return Instance.enabledFeatures.EmptyIfNull().Contains(feature);
        }

        public static bool GeneOfTypeExists<T>()
        {
            bool result = DefDatabase<GeneDef>.AllDefs.Any(gene => gene.geneClass.IsAssignableFrom(typeof(T)));
            Log.Message($"XylRacesCore feature check: {typeof(T)} = {result}");
            return result;
        }

        public static bool GeneWithModExtensionExists<T>()
        {
            bool result = DefDatabase<GeneDef>.AllDefs.Where(gene => gene.modExtensions != null).Any(gene => gene.modExtensions.OfType<T>().Any());
            Log.Message($"XylRacesCore feature check: {typeof(T)} = {result}");
            return result;
        }
    }
}
