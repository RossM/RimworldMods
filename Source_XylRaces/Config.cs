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
            Joyless,
            UIChange,
        }

        public static Config Instance => instance ??= DefDatabase<Config>.AllDefs.Single();

        private static Config instance;
        public List<JobDef> wetnessGivingJobs;

        [NoTranslate] public List<string> ignoreGenesFromMods;

        public static bool GeneOfTypeExists<T>()
        {
            bool result = DefDatabase<GeneDef>.AllDefs.Any(gene => typeof(T).IsAssignableFrom(gene.geneClass));
            Log.Message($"XylXenos feature check: {typeof(T)} = {result}");
            return result;
        }

        public static bool GeneWithModExtensionExists<T>()
        {
            bool result = DefDatabase<GeneDef>.AllDefs.Any(gene => gene.modExtensions?.OfType<T>().Any() == true);
            Log.Message($"XylXenos feature check: {typeof(T)} = {result}");
            return result;
        }
    }
}
