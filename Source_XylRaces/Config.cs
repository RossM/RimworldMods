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
    }
}
