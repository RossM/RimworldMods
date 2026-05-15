using System.Collections.Generic;
using System.Linq;
using Verse;

namespace XylRacesCore;

public static class Scribe_Ext
{
    public static void Look<T1, T2>(ref HashSet<(T1, T2)> valueTuples, string label, LookMode lookMode)
    {
        List<T1> listFirst = new();
        List<T2> listSecond = new();
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            foreach (var pair in valueTuples)
            {
                listFirst.Add(pair.Item1);
                listSecond.Add(pair.Item2);
            }
        }

        if (Scribe.EnterNode(label))
        {
            try
            {
                Scribe_Collections.Look(ref listFirst, "first", lookMode);
                Scribe_Collections.Look(ref listSecond, "second", lookMode);
            }
            finally
            {
                Scribe.ExitNode();
            }
        }

        if ((lookMode == LookMode.Reference && Scribe.mode == LoadSaveMode.ResolvingCrossRefs) ||
            (lookMode != LookMode.Reference && Scribe.mode == LoadSaveMode.LoadingVars))
        {
            valueTuples = [];
            if (listFirst != null && listSecond != null)
            {
                foreach (var pair in listFirst.Zip(listSecond, (f, s) => (f, s)))
                    valueTuples.Add(pair);
            }
        }
    }
}
