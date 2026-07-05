using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XylXenos
{
    public class GeneSetMakerDef : Def
    {
        public GeneSetMaker root;

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            root.ResolveReferences();
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
                yield return error;

            if (root == null)
            {
                yield return "root is null";
                yield break;
            }

            foreach (var error in root.ConfigErrors())
                yield return error;
        }
    }
}
